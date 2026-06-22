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
        /// Belirtilen lot numarasına ait bir işlemi blokzincirine kaydeder.
        /// </summary>
        /// <param name="lotNumber">Parti numarası</param>
        /// <param name="actionType">0=Added, 1=Deducted, 2=Produced, 3=Shipped, 4=Received, 5=Transferred, 6=Consumed, 7=Genesis</param>
        /// <param name="quantity">Miktar</param>
        /// <param name="fromLocation">Kaynak konum (ör: "Üretici - Abbott")</param>
        /// <param name="toLocation">Hedef konum (ör: "Merkez Depo")</param>
        /// <returns>İşlemin Blockchain Transaction Hash değeri. İşlem başarısızsa hata fırlatır.</returns>
        Task<string> LogActionAsync(string lotNumber, int actionType, int quantity, string fromLocation = "", string toLocation = "");

        /// <summary>
        /// Birden fazla lot hareketini tek blockchain transaction'ı içinde atomik olarak kaydeder.
        /// </summary>
        /// <param name="actions">Kaydedilecek lot hareketleri</param>
        /// <returns>Batch işleminin Transaction Hash değeri. İşlem başarısızsa hata fırlatır.</returns>
        Task<string> LogActionsAsync(IEnumerable<BlockchainActionRequest> actions);

        /// <summary>
        /// Belirtilen lot numarasının blokzincirindeki tüm geçmişini getirir.
        /// </summary>
        /// <param name="lotNumber">Parti numarası</param>
        /// <returns>Blokzinciri kayıt listesi</returns>
        Task<List<BlockchainRecord>> GetHistoryAsync(string lotNumber);

        /// <summary>
        /// Belirtilen lot numarasının blokzinciri üzerindeki güncel durumunu getirir.
        /// </summary>
        /// <param name="lotNumber">Parti numarası</param>
        /// <returns>Lotun zincir üzerindeki stok ve durum bilgisi</returns>
        Task<BlockchainLotStatus> GetLotStatusAsync(string lotNumber);

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
        public string Action { get; set; } = string.Empty;      // "Added", "Deducted", "Produced", "Shipped", "Received", "Transferred", "Consumed", "Genesis"
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserAddress { get; set; } = string.Empty;  // Cüzdan adresi
        public string FromLocation { get; set; } = string.Empty; // Kaynak konum
        public string ToLocation { get; set; } = string.Empty;   // Hedef konum
    }

    public class BlockchainLotStatus
    {
        public string LotNumber { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public string State { get; set; } = string.Empty;
        public int OnChainQuantity { get; set; }
        public int PendingQuantity { get; set; }
    }

    public class BlockchainActionRequest
    {
        public string LotNumber { get; set; } = string.Empty;
        public int ActionType { get; set; }
        public int Quantity { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
    }
}
