using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace NumuneStok.Services
{
    /// <summary>
    /// Nethereum kullanarak Hardhat yerel ağındaki SupplyChainLedger sözleşmesiyle iletişim kurar.
    /// </summary>
    public class BlockchainService : IBlockchainService
    {
        private readonly Web3 _web3;
        private readonly string _contractAddress;
        private readonly ILogger<BlockchainService> _logger;

        // Action type isimlerini enum değerlerine eşleyen yardımcı dizi
        private static readonly string[] ActionTypeNames = 
        { 
            "Added",        // 0
            "Deducted",     // 1
            "Produced",     // 2
            "Shipped",      // 3
            "Received",     // 4
            "Transferred",  // 5
            "Consumed",     // 6
            "Genesis"       // 7
        };

        private static readonly string[] LotStateNames =
        {
            "None",
            "Produced",
            "Shipped",
            "Received",
            "Transferred",
            "Consumed"
        };

        // SupplyChainLedger sözleşmesinin ABI tanımı (güncellenmiş — fromLocation/toLocation destekli)
        private const string ContractABI = @"[
            {
                ""anonymous"": false,
                ""inputs"": [
                    { ""indexed"": true, ""internalType"": ""string"", ""name"": ""lotNumber"", ""type"": ""string"" },
                    { ""indexed"": false, ""internalType"": ""enum SupplyChainLedger.ActionType"", ""name"": ""action"", ""type"": ""uint8"" },
                    { ""indexed"": false, ""internalType"": ""uint256"", ""name"": ""quantity"", ""type"": ""uint256"" },
                    { ""indexed"": false, ""internalType"": ""uint256"", ""name"": ""timestamp"", ""type"": ""uint256"" },
                    { ""indexed"": true, ""internalType"": ""address"", ""name"": ""user"", ""type"": ""address"" },
                    { ""indexed"": false, ""internalType"": ""string"", ""name"": ""fromLocation"", ""type"": ""string"" },
                    { ""indexed"": false, ""internalType"": ""string"", ""name"": ""toLocation"", ""type"": ""string"" }
                ],
                ""name"": ""StateChanged"",
                ""type"": ""event""
            },
            {
                ""inputs"": [
                    { ""internalType"": ""string"", ""name"": ""_lotNumber"", ""type"": ""string"" }
                ],
                ""name"": ""getLotStatus"",
                ""outputs"": [
                    { ""internalType"": ""bool"", ""name"": ""exists"", ""type"": ""bool"" },
                    { ""internalType"": ""enum SupplyChainLedger.LotState"", ""name"": ""state"", ""type"": ""uint8"" },
                    { ""internalType"": ""uint256"", ""name"": ""onChainQuantity"", ""type"": ""uint256"" },
                    { ""internalType"": ""uint256"", ""name"": ""pendingQuantity"", ""type"": ""uint256"" }
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    { ""internalType"": ""string"", ""name"": ""_lotNumber"", ""type"": ""string"" }
                ],
                ""name"": ""getHistory"",
                ""outputs"": [
                    {
                        ""components"": [
                            { ""internalType"": ""string"", ""name"": ""lotNumber"", ""type"": ""string"" },
                            { ""internalType"": ""enum SupplyChainLedger.ActionType"", ""name"": ""action"", ""type"": ""uint8"" },
                            { ""internalType"": ""uint256"", ""name"": ""quantity"", ""type"": ""uint256"" },
                            { ""internalType"": ""uint256"", ""name"": ""timestamp"", ""type"": ""uint256"" },
                            { ""internalType"": ""address"", ""name"": ""user"", ""type"": ""address"" },
                            { ""internalType"": ""string"", ""name"": ""fromLocation"", ""type"": ""string"" },
                            { ""internalType"": ""string"", ""name"": ""toLocation"", ""type"": ""string"" }
                        ],
                        ""internalType"": ""struct SupplyChainLedger.Record[]"",
                        ""name"": """",
                        ""type"": ""tuple[]""
                    }
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    { ""internalType"": ""string"", ""name"": ""_lotNumber"", ""type"": ""string"" },
                    { ""internalType"": ""enum SupplyChainLedger.ActionType"", ""name"": ""_action"", ""type"": ""uint8"" },
                    { ""internalType"": ""uint256"", ""name"": ""_quantity"", ""type"": ""uint256"" },
                    { ""internalType"": ""string"", ""name"": ""_fromLocation"", ""type"": ""string"" },
                    { ""internalType"": ""string"", ""name"": ""_toLocation"", ""type"": ""string"" }
                ],
                ""name"": ""logAction"",
                ""outputs"": [],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    { ""internalType"": ""string[]"", ""name"": ""_lotNumbers"", ""type"": ""string[]"" },
                    { ""internalType"": ""enum SupplyChainLedger.ActionType[]"", ""name"": ""_actions"", ""type"": ""uint8[]"" },
                    { ""internalType"": ""uint256[]"", ""name"": ""_quantities"", ""type"": ""uint256[]"" },
                    { ""internalType"": ""string[]"", ""name"": ""_fromLocations"", ""type"": ""string[]"" },
                    { ""internalType"": ""string[]"", ""name"": ""_toLocations"", ""type"": ""string[]"" }
                ],
                ""name"": ""logActions"",
                ""outputs"": [],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            }
        ]";

        public BlockchainService(IConfiguration configuration, ILogger<BlockchainService> logger)
        {
            _logger = logger;

            // appsettings.json dosyasından blockchain ayarlarını oku
            var rpcUrl = configuration["Blockchain:RpcUrl"] ?? "http://127.0.0.1:8545";
            var privateKey = configuration["Blockchain:PrivateKey"] 
                ?? throw new InvalidOperationException("Blockchain:PrivateKey appsettings.json dosyasında tanımlı değil.");
            _contractAddress = configuration["Blockchain:ContractAddress"] 
                ?? throw new InvalidOperationException("Blockchain:ContractAddress appsettings.json dosyasında tanımlı değil.");

            // Hardhat'in varsayılan hesabıyla Web3 bağlantısı kur
            var account = new Account(privateKey);
            _web3 = new Web3(account, rpcUrl);

            _logger.LogInformation("BlockchainService başlatıldı. RPC: {RpcUrl}, Sözleşme: {ContractAddress}", rpcUrl, _contractAddress);
        }

        /// <inheritdoc />
        public async Task<string> LogActionAsync(string lotNumber, int actionType, int quantity, string fromLocation = "", string toLocation = "")
        {
            try
            {
                ValidateAction(lotNumber, actionType, quantity);

                var contract = _web3.Eth.GetContract(ContractABI, _contractAddress);
                var logActionFunction = contract.GetFunction("logAction");

                // İşlemi blokzincirine gönder ve Transaction Hash'i al
                var txHash = await logActionFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    new HexBigInteger(500000), // Gas limiti (artırıldı — string parametreler daha fazla gas tüketir)
                    null,                      // Value (ETH göndermiyoruz)
                    lotNumber,
                    actionType,
                    quantity,
                    fromLocation,
                    toLocation
                );

                await EnsureTransactionSucceededAsync(txHash);

                var actionName = actionType >= 0 && actionType < ActionTypeNames.Length 
                    ? ActionTypeNames[actionType] 
                    : actionType.ToString();

                _logger.LogInformation(
                    "Blockchain kaydı oluşturuldu. LotNumber: {LotNumber}, Action: {Action}, From: {From}, To: {To}, TxHash: {TxHash}",
                    lotNumber, actionName, fromLocation, toLocation, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain'e kayıt gönderilirken hata oluştu. LotNumber: {LotNumber}", lotNumber);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> LogActionsAsync(IEnumerable<BlockchainActionRequest> actions)
        {
            var actionList = actions.ToList();
            if (actionList.Count == 0)
            {
                return string.Empty;
            }

            foreach (var action in actionList)
            {
                ValidateAction(action.LotNumber, action.ActionType, action.Quantity);
            }

            if (actionList.Count == 1)
            {
                var action = actionList[0];
                return await LogActionAsync(
                    action.LotNumber,
                    action.ActionType,
                    action.Quantity,
                    action.FromLocation,
                    action.ToLocation
                );
            }

            try
            {
                var contract = _web3.Eth.GetContract(ContractABI, _contractAddress);
                var logActionsFunction = contract.GetFunction("logActions");

                var txHash = await logActionsFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    new HexBigInteger(2000000),
                    null,
                    actionList.Select(a => a.LotNumber).ToArray(),
                    actionList.Select(a => new BigInteger(a.ActionType)).ToArray(),
                    actionList.Select(a => new BigInteger(a.Quantity)).ToArray(),
                    actionList.Select(a => a.FromLocation).ToArray(),
                    actionList.Select(a => a.ToLocation).ToArray()
                );

                await EnsureTransactionSucceededAsync(txHash);

                _logger.LogInformation(
                    "Blockchain batch kaydı oluşturuldu. Count: {Count}, TxHash: {TxHash}",
                    actionList.Count, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain batch kaydı oluşturulurken hata oluştu. Count: {Count}", actionList.Count);
                throw;
            }
        }

        private static void ValidateAction(string lotNumber, int actionType, int quantity)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                throw new ArgumentException("Lot numarası boş olamaz.", nameof(lotNumber));
            }

            if (actionType < 0 || actionType >= ActionTypeNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(actionType), "Geçersiz blockchain işlem tipi.");
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Miktar sıfırdan büyük olmalıdır.");
            }
        }

        private async Task EnsureTransactionSucceededAsync(string txHash)
        {
            var receipt = await WaitForReceiptAsync(txHash);

            if (receipt.Status != null && receipt.Status.Value == 0)
            {
                throw new InvalidOperationException($"Blockchain işlemi başarısız oldu. TxHash: {txHash}");
            }
        }

        private async Task<TransactionReceipt> WaitForReceiptAsync(string txHash)
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                if (receipt != null)
                {
                    return receipt;
                }

                await Task.Delay(500);
            }

            throw new TimeoutException($"Blockchain işlem makbuzu zamanında alınamadı. TxHash: {txHash}");
        }

        /// <inheritdoc />
        public async Task<List<BlockchainRecord>> GetHistoryAsync(string lotNumber)
        {
            var records = new List<BlockchainRecord>();

            try
            {
                var contract = _web3.Eth.GetContract(ContractABI, _contractAddress);
                var getHistoryFunction = contract.GetFunction("getHistory");

                // Sözleşmeden geçmişi oku (view fonksiyonu - gas harcamaz)
                var result = await getHistoryFunction.CallDeserializingToObjectAsync<GetHistoryOutputDTO>(lotNumber);

                if (result?.Records != null)
                {
                    foreach (var record in result.Records)
                    {
                        var actionName = record.Action >= 0 && record.Action < ActionTypeNames.Length 
                            ? ActionTypeNames[record.Action] 
                            : "Unknown";

                        records.Add(new BlockchainRecord
                        {
                            LotNumber = record.LotNumber,
                            Action = actionName,
                            Quantity = (int)record.Quantity,
                            Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)record.Timestamp).DateTime,
                            UserAddress = record.User,
                            FromLocation = record.FromLocation ?? "",
                            ToLocation = record.ToLocation ?? ""
                        });
                    }
                }

                _logger.LogInformation("Blockchain geçmişi alındı. LotNumber: {LotNumber}, Kayıt sayısı: {Count}", lotNumber, records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain geçmişi alınırken hata oluştu. LotNumber: {LotNumber}", lotNumber);
                throw;
            }

            return records;
        }

        /// <inheritdoc />
        public async Task<BlockchainLotStatus> GetLotStatusAsync(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                throw new ArgumentException("Lot numarası boş olamaz.", nameof(lotNumber));
            }

            try
            {
                var contract = _web3.Eth.GetContract(ContractABI, _contractAddress);
                var getLotStatusFunction = contract.GetFunction("getLotStatus");
                var result = await getLotStatusFunction.CallDeserializingToObjectAsync<GetLotStatusOutputDTO>(lotNumber);

                var stateName = result.State >= 0 && result.State < LotStateNames.Length
                    ? LotStateNames[result.State]
                    : "Unknown";

                return new BlockchainLotStatus
                {
                    LotNumber = lotNumber,
                    Exists = result.Exists,
                    State = stateName,
                    OnChainQuantity = (int)result.OnChainQuantity,
                    PendingQuantity = (int)result.PendingQuantity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain lot durumu alınırken hata oluştu. LotNumber: {LotNumber}", lotNumber);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsBlockchainAvailableAsync()
        {
            try
            {
                // Ağdan blok numarasını çekmeyi dener. Başarılı olursa ağ ayaktadır.
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Blockchain ağına erişilemiyor: {ex.Message}");
                return false;
            }
        }
    }

    // ─── Nethereum DTO sınıfları (ABI Deserialization) ───

    [FunctionOutput]
    public class GetHistoryOutputDTO : IFunctionOutputDTO
    {
        [Parameter("tuple[]", "", 1)]
        public List<RecordDTO> Records { get; set; } = new();
    }

    public class RecordDTO
    {
        [Parameter("string", "lotNumber", 1)]
        public string LotNumber { get; set; } = string.Empty;

        [Parameter("uint8", "action", 2)]
        public int Action { get; set; }

        [Parameter("uint256", "quantity", 3)]
        public BigInteger Quantity { get; set; }

        [Parameter("uint256", "timestamp", 4)]
        public BigInteger Timestamp { get; set; }

        [Parameter("address", "user", 5)]
        public string User { get; set; } = string.Empty;

        [Parameter("string", "fromLocation", 6)]
        public string FromLocation { get; set; } = string.Empty;

        [Parameter("string", "toLocation", 7)]
        public string ToLocation { get; set; } = string.Empty;
    }

    [FunctionOutput]
    public class GetLotStatusOutputDTO : IFunctionOutputDTO
    {
        [Parameter("bool", "exists", 1)]
        public bool Exists { get; set; }

        [Parameter("uint8", "state", 2)]
        public int State { get; set; }

        [Parameter("uint256", "onChainQuantity", 3)]
        public BigInteger OnChainQuantity { get; set; }

        [Parameter("uint256", "pendingQuantity", 4)]
        public BigInteger PendingQuantity { get; set; }
    }
}
