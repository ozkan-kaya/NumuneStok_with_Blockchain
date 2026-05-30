using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;

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

        // SupplyChainLedger sözleşmesinin ABI tanımı
        private const string ContractABI = @"[
            {
                ""anonymous"": false,
                ""inputs"": [
                    { ""indexed"": true, ""internalType"": ""string"", ""name"": ""lotNumber"", ""type"": ""string"" },
                    { ""indexed"": false, ""internalType"": ""enum SupplyChainLedger.ActionType"", ""name"": ""action"", ""type"": ""uint8"" },
                    { ""indexed"": false, ""internalType"": ""uint256"", ""name"": ""quantity"", ""type"": ""uint256"" },
                    { ""indexed"": false, ""internalType"": ""uint256"", ""name"": ""timestamp"", ""type"": ""uint256"" },
                    { ""indexed"": true, ""internalType"": ""address"", ""name"": ""user"", ""type"": ""address"" }
                ],
                ""name"": ""StateChanged"",
                ""type"": ""event""
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
                            { ""internalType"": ""address"", ""name"": ""user"", ""type"": ""address"" }
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
                    { ""internalType"": ""uint256"", ""name"": ""_quantity"", ""type"": ""uint256"" }
                ],
                ""name"": ""logAction"",
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
        public async Task<string> LogActionAsync(string lotNumber, int actionType, int quantity)
        {
            try
            {
                var contract = _web3.Eth.GetContract(ContractABI, _contractAddress);
                var logActionFunction = contract.GetFunction("logAction");

                // İşlemi blokzincirine gönder ve Transaction Hash'i al
                var txHash = await logActionFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    new HexBigInteger(300000), // Gas limiti
                    null,                      // Value (ETH göndermiyoruz)
                    lotNumber,
                    actionType,
                    quantity
                );

                _logger.LogInformation(
                    "Blockchain kaydı oluşturuldu. LotNumber: {LotNumber}, Action: {Action}, TxHash: {TxHash}",
                    lotNumber, actionType == 0 ? "Added" : "Deducted", txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain'e kayıt gönderilirken hata oluştu. LotNumber: {LotNumber}", lotNumber);
                // Blockchain hatası ana iş akışını durdurmamalı
                return string.Empty;
            }
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
                        records.Add(new BlockchainRecord
                        {
                            LotNumber = record.LotNumber,
                            Action = record.Action == 0 ? "Added" : "Deducted",
                            Quantity = (int)record.Quantity,
                            Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)record.Timestamp).DateTime,
                            UserAddress = record.User
                        });
                    }
                }

                _logger.LogInformation("Blockchain geçmişi alındı. LotNumber: {LotNumber}, Kayıt sayısı: {Count}", lotNumber, records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain geçmişi alınırken hata oluştu. LotNumber: {LotNumber}", lotNumber);
            }

            return records;
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
    }
}
