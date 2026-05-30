using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumuneStok.Services
{
    /// <summary>
    /// Blockchain üzerindeki SupplyChainLedger sözleşmesi ile iletişim kuran servis arayüzü.
    /// </summary>
    public interface IBlockchainService
    {
        /// <summary>
        /// Belirtilen lot numarasına ait bir işlemi (Ekleme veya Düşme) blokzincirine kaydeder.
        /// </summary>
        /// <param name="lotNumber">Parti numarası</param>
        /// <param name="actionType">0 = Added (Eklendi), 1 = Deducted (Düşüldü)</param>
        /// <returns>İşlemin Blockchain Transaction Hash değeri</returns>
        Task<string> LogActionAsync(string lotNumber, int actionType, int quantity);

        /// <summary>
        /// Belirtilen lot numarasının blokzincirindeki tüm geçmişini getirir.
        /// </summary>
        /// <param name="lotNumber">Parti numarası</param>
        /// <returns>Blokzinciri kayıt listesi</returns>
        Task<List<BlockchainRecord>> GetHistoryAsync(string lotNumber);

        /// <summary>
        /// Blokzinciri ağının erişilebilir olup olmadığını kontrol eder.
        /// </summary>
        /// <returns>Ağ ayaktaysa true, değilse false</returns>
        Task<bool> IsBlockchainAvailableAsync();
    }

    /// <summary>
    /// Blokzincirinden dönen kayıt modeli.
    /// </summary>
    public class BlockchainRecord
    {
        public string LotNumber { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;      // "Added" veya "Deducted"
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserAddress { get; set; } = string.Empty;  // Cüzdan adresi
    }
}
