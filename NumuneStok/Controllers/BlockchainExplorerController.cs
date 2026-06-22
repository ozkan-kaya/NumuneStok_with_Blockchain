using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;
using NumuneStok.Services;
using System.Globalization;
using System.Text;

namespace NumuneStok.Controllers
{
    [Authorize]
    public class BlockchainExplorerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlockchainService _blockchainService;
        private readonly ILogger<BlockchainExplorerController> _logger;

        public BlockchainExplorerController(
            ApplicationDbContext context,
            IBlockchainService blockchainService,
            ILogger<BlockchainExplorerController> logger)
        {
            _context = context;
            _blockchainService = blockchainService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm lot kayıtlarını MySQL ile blockchain arasında karşılaştırarak
        /// veri bütünlüğü uyuşmazlıklarını listeler.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var rows = new List<IntegrityCheckRow>();
            bool isBlockchainUp = false;
            string? networkError = null;

            try
            {
                isBlockchainUp = await _blockchainService.IsBlockchainAvailableAsync();
            }
            catch (Exception ex)
            {
                networkError = ex.Message;
            }

            ViewBag.BlockchainAvailable = isBlockchainUp;
            ViewBag.NetworkError = networkError;

            if (!isBlockchainUp)
            {
                return View(rows);
            }

            // Tüm pozitif stoklu ve lot numarası olan kayıtları çek
            var childProducts = await _context.ChildProducts
                .AsNoTracking()
                .Include(cp => cp.Product)
                .Where(cp => !string.IsNullOrWhiteSpace(cp.LotNumber))
                .ToListAsync();

            // Lot bazında grupla, toplam stokları hesapla
            var lotSnapshots = childProducts
                .GroupBy(cp => NormalizeLotNumber(cp.LotNumber))
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => new
                {
                    LotNumber = g.Key,
                    DatabaseStock = g.Sum(cp => cp.Quantity),
                    ProductName = g.First().Product?.ProductName ?? "—"
                })
                .ToList();

            foreach (var lot in lotSnapshots)
            {
                var row = new IntegrityCheckRow
                {
                    LotNumber = lot.LotNumber,
                    ProductName = lot.ProductName,
                    DatabaseStock = lot.DatabaseStock
                };

                try
                {
                    var status = await _blockchainService.GetLotStatusAsync(lot.LotNumber);
                    row.BlockchainExists = status.Exists;
                    row.BlockchainStock = status.OnChainQuantity;
                    row.BlockchainState = status.State;
                    row.PendingQuantity = status.PendingQuantity;

                    // Uyuşmazlık sınıflandırması
                    if (!status.Exists && lot.DatabaseStock > 0)
                    {
                        row.Status = IntegrityStatus.GenesisEksik;
                    }
                    else if (!status.Exists && lot.DatabaseStock == 0)
                    {
                        row.Status = IntegrityStatus.Eslesme;
                    }
                    else if (status.OnChainQuantity == lot.DatabaseStock)
                    {
                        row.Status = IntegrityStatus.Eslesme;
                    }
                    else if (status.OnChainQuantity > lot.DatabaseStock)
                    {
                        row.Status = IntegrityStatus.BlockchainFazla;
                    }
                    else
                    {
                        row.Status = IntegrityStatus.VeritabaniFazla;
                    }
                }
                catch (Exception ex)
                {
                    row.Status = IntegrityStatus.HataVar;
                    row.ErrorMessage = ex.Message;
                    _logger.LogWarning(ex, "Lot {LotNumber} blockchain durumu alınamadı.", lot.LotNumber);
                }

                rows.Add(row);
            }

            // Uyuşmazlıkları üste, eşleşenleri alta sırala
            rows = rows
                .OrderByDescending(r => r.Status != IntegrityStatus.Eslesme)
                .ThenBy(r => r.LotNumber)
                .ToList();

            ViewBag.TotalLots = rows.Count;
            ViewBag.MatchCount = rows.Count(r => r.Status == IntegrityStatus.Eslesme);
            ViewBag.MismatchCount = rows.Count(r => r.Status != IntegrityStatus.Eslesme && r.Status != IntegrityStatus.HataVar);
            ViewBag.ErrorCount = rows.Count(r => r.Status == IntegrityStatus.HataVar);

            return View(rows);
        }

        /// <summary>
        /// Seçilen lot için tam blockchain geçmişini döner (AJAX endpoint).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LotDetail(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                return BadRequest("Lot numarası gereklidir.");
            }

            lotNumber = NormalizeLotNumber(lotNumber);

            try
            {
                var history = await _blockchainService.GetHistoryAsync(lotNumber);
                var status = await _blockchainService.GetLotStatusAsync(lotNumber);
                return Json(new { success = true, history, status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lot {LotNumber} detayı alınamadı.", lotNumber);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static string NormalizeLotNumber(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                return string.Empty;
            }

            var normalized = lotNumber.Normalize(NormalizationForm.FormKC);
            var cleaned = normalized.Where(c =>
                !char.IsWhiteSpace(c) &&
                !char.IsControl(c) &&
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format);

            return new string(cleaned.ToArray()).Trim();
        }
    }

    // ─── View Model ───

    public enum IntegrityStatus
    {
        Eslesme,        // DB stok == Blockchain stok
        GenesisEksik,   // DB'de stok var ama blockchain'de kayıt yok
        VeritabaniFazla, // DB stok > Blockchain stok (olası manipülasyon)
        BlockchainFazla, // Blockchain stok > DB stok
        HataVar         // Blockchain'den veri alınırken hata
    }

    public class IntegrityCheckRow
    {
        public string LotNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int DatabaseStock { get; set; }
        public bool BlockchainExists { get; set; }
        public int BlockchainStock { get; set; }
        public string BlockchainState { get; set; } = string.Empty;
        public int PendingQuantity { get; set; }
        public IntegrityStatus Status { get; set; }
        public string? ErrorMessage { get; set; }

        public int StockDifference => DatabaseStock - BlockchainStock;
    }
}
