using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;
using System.Globalization;
using System.Text;

namespace NumuneStok.Services
{
    public interface IBlockchainStartupStockSyncService
    {
        bool HasCompletedSuccessfully { get; }
        StartupStockSyncResult? LastResult { get; }
        Task<StartupStockSyncResult> SynchronizeAsync(bool force = false, CancellationToken cancellationToken = default);
    }

    public sealed class StartupStockSyncResult
    {
        public bool Succeeded { get; set; }
        public int LotCount { get; set; }
        public int InitializedCount { get; set; }
        public int CompletedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BlockchainStartupStockSyncService : IBlockchainStartupStockSyncService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BlockchainStartupStockSyncService> _logger;
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        public BlockchainStartupStockSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<BlockchainStartupStockSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool HasCompletedSuccessfully => LastResult?.Succeeded == true;
        public StartupStockSyncResult? LastResult { get; private set; }

        public async Task<StartupStockSyncResult> SynchronizeAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            if (!force && HasCompletedSuccessfully)
            {
                return LastResult!;
            }

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                if (!force && HasCompletedSuccessfully)
                {
                    return LastResult!;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

                if (!await blockchainService.IsBlockchainAvailableAsync())
                {
                    LastResult = new StartupStockSyncResult
                    {
                        Succeeded = false,
                        ErrorMessage = "Blockchain ağı erişilemez durumda."
                    };

                    return LastResult;
                }

                var lots = await context.ChildProducts
                    .AsNoTracking()
                    .Where(childProduct => childProduct.Quantity > 0)
                    .ToListAsync(cancellationToken);

                var snapshots = BuildPositiveLotSnapshots(lots);
                var initializedCount = 0;
                var completedCount = 0;

                foreach (var snapshot in snapshots)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var existingHistory = await blockchainService.GetHistoryAsync(snapshot.LotNumber);
                    if (existingHistory.Count == 0)
                    {
                        await blockchainService.LogActionAsync(
                            snapshot.LotNumber,
                            7, // Genesis
                            snapshot.Quantity,
                            "Sistem Başlangıç Envanteri",
                            "Merkez Depo");

                        initializedCount++;
                        continue;
                    }

                    var blockchainStock = CalculateBlockchainStock(existingHistory);
                    if (blockchainStock < snapshot.Quantity)
                    {
                        await blockchainService.LogActionAsync(
                            snapshot.LotNumber,
                            0, // Added
                            snapshot.Quantity - blockchainStock,
                            "Sistem Başlangıç Envanteri Tamamlama",
                            "Merkez Depo");

                        completedCount++;
                    }
                }

                LastResult = new StartupStockSyncResult
                {
                    Succeeded = true,
                    LotCount = snapshots.Count,
                    InitializedCount = initializedCount,
                    CompletedCount = completedCount
                };

                _logger.LogInformation(
                    "Blockchain başlangıç stoğu senkronize edildi. Lot: {LotCount}, yeni başlangıç kaydı: {InitializedCount}, tamamlanan kayıt: {CompletedCount}",
                    LastResult.LotCount,
                    LastResult.InitializedCount,
                    LastResult.CompletedCount);

                return LastResult;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastResult = new StartupStockSyncResult
                {
                    Succeeded = false,
                    ErrorMessage = ex.Message
                };

                _logger.LogWarning(ex, "Blockchain başlangıç stoğu senkronize edilemedi.");
                return LastResult;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private static List<InitialLotSnapshot> BuildPositiveLotSnapshots(IEnumerable<ChildProduct> productLots)
        {
            return productLots
                .Where(childProduct => childProduct.Quantity > 0 && !string.IsNullOrWhiteSpace(childProduct.LotNumber))
                .Select(childProduct => new
                {
                    LotNumber = NormalizeLotNumber(childProduct.LotNumber),
                    childProduct.Quantity
                })
                .Where(childProduct => childProduct.Quantity > 0 && !string.IsNullOrEmpty(childProduct.LotNumber))
                .GroupBy(childProduct => childProduct.LotNumber)
                .Select(group => new InitialLotSnapshot
                {
                    LotNumber = group.Key,
                    Quantity = group.Sum(childProduct => childProduct.Quantity)
                })
                .Where(snapshot => snapshot.Quantity > 0)
                .ToList();
        }

        private static int CalculateBlockchainStock(IEnumerable<BlockchainRecord> records)
        {
            var stock = 0;

            foreach (var record in records)
            {
                if (record.Action == "Added" || record.Action == "Received" || record.Action == "Genesis")
                {
                    stock += record.Quantity;
                }
                else if (record.Action == "Deducted" || (record.Action == "Consumed" && record.ToLocation == "Tüketildi"))
                {
                    stock -= record.Quantity;
                }
            }

            return stock;
        }

        private static string NormalizeLotNumber(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                return string.Empty;
            }

            var normalized = lotNumber.Normalize(NormalizationForm.FormKC);
            var cleanedCharacters = normalized.Where(character =>
                !char.IsWhiteSpace(character) &&
                !char.IsControl(character) &&
                CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.Format);

            return new string(cleanedCharacters.ToArray()).Trim();
        }

        private sealed class InitialLotSnapshot
        {
            public string LotNumber { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }

    public class BlockchainStartupStockInitializer : BackgroundService
    {
        private const int MaxStartupAttempts = 12;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly IBlockchainStartupStockSyncService _syncService;
        private readonly ILogger<BlockchainStartupStockInitializer> _logger;

        public BlockchainStartupStockInitializer(
            IBlockchainStartupStockSyncService syncService,
            ILogger<BlockchainStartupStockInitializer> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            for (var attempt = 1; attempt <= MaxStartupAttempts && !stoppingToken.IsCancellationRequested; attempt++)
            {
                var result = await _syncService.SynchronizeAsync(cancellationToken: stoppingToken);
                if (result.Succeeded)
                {
                    return;
                }

                _logger.LogWarning(
                    "Blockchain başlangıç stoğu yüklenemedi. Deneme: {Attempt}/{MaxAttempts}. Hata: {ErrorMessage}",
                    attempt,
                    MaxStartupAttempts,
                    result.ErrorMessage);

                if (attempt < MaxStartupAttempts)
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }

            _logger.LogError("Blockchain başlangıç stoğu yüklenemedi. Uygulama çalışıyor, ancak stok doğrulama ekranında uyuşmazlık görünebilir.");
        }
    }
}
