<div align="center">

# 🔬 NumuneStok

### Blockchain Tabanlı Laboratuvar Numune Stok ve Tedarik Zinciri Yönetim Sistemi

_Lot bazlı numune hareketlerinin akıllı sözleşme kurallarıyla doğrulanabilir, değiştirilemez kayıt defterinde izlenmesi_

---

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Solidity](https://img.shields.io/badge/Solidity-0.8.24-363636?style=for-the-badge&logo=solidity)
![Hardhat](https://img.shields.io/badge/Hardhat-2.22-F7DF1E?style=for-the-badge&logo=ethereum)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![C#](https://img.shields.io/badge/C%23-ASP.NET_MVC-239120?style=for-the-badge&logo=csharp)

</div>

---

## 📖 Proje Hakkında

**NumuneStok**, klinik laboratuvar ortamlarında kullanılan reagent, kit ve numune malzemelerinin lot bazlı stok hareketlerini hem geleneksel bir ilişkisel veritabanında hem de lokal izinli bir blockchain ağı üzerinde eş zamanlı olarak kaydeden bir **ASP.NET Core MVC** prototipidir.

Sistem iki katmanlı bir mimari kullanır:

| Katman                  | Teknoloji                      | Rol                                             |
| ----------------------- | ------------------------------ | ----------------------------------------------- |
| **Operasyonel Veri**    | MySQL + Entity Framework Core  | Hızlı sorgulama, ürün ve kullanıcı yönetimi     |
| **Denetim & Doğrulama** | Solidity + Hardhat + Nethereum | Değiştirilemez denetim izi, iş kuralı zorlaması |

> **Not:** Bu proje **public blockchain** kullanmamaktadır. Lokal Hardhat ağı, izinli/özel blockchain prototipi olarak akademik amaçla kullanılmıştır.

---

## 🏗️ Mimari

```
┌──────────────────────────────────────────────────────────┐
│                   Kullanıcı (Tarayıcı)                    │
└───────────────────────────┬──────────────────────────────┘
                            │ HTTP
┌───────────────────────────▼──────────────────────────────┐
│              ASP.NET Core MVC (.NET 8)                    │
│                                                          │
│  ProductController  ←→  BlockchainService (Nethereum)    │
│  BlockchainExplorerController                            │
│           │                        │                    │
│    MySQL / EF Core          Hardhat Lokal Node           │
│    (Operasyonel Veri)       JSON-RPC :8545               │
│                                    │                    │
│                         SupplyChainLedger.sol            │
│                         (Akıllı Sözleşme)               │
└──────────────────────────────────────────────────────────┘
```

---

## ✨ Özellikler

### 🔗 Blockchain Katmanı

- **Durum Makinesi:** Her lot `None → Produced → Shipped → Received → Transferred → Consumed` zincirini izler; sıra dışı işlemler sözleşme tarafından reddedilir
- **Rol Tabanlı Yetkilendirme:** Producer, Warehouse, Laboratory ve Admin rolleri akıllı sözleşme seviyesinde ayrıştırılmıştır
- **Değiştirilemez Denetim İzi:** Her işlem transaction hash ile zincire kalıcı yazılır
- **Toplu İşlem (Batch):** `logActions()` ile birden fazla lot tek transaction'da yazılır
- **Kontrollü Başlangıç Senkronizasyonu:** Mevcut stoklar yalnızca `Blockchain/scripts/start_and_sync.sh` üzerinden Genesis kaydıyla blockchain'e yazılır

### 🗄️ Operasyonel Katman

- Lot bazlı stok takibi (lot numarası, son kullanma tarihi, miktar)
- Tedarik zinciri aktörleri: Supplier, Carrier, WarehouseLocation, LaboratoryLocation
- Sevkiyat, transfer ve teslim alma süreç kayıtları
- Kritik stok ve son kullanma tarihi uyarıları
- Barkod ile stok hareketi

### 🔍 Bütünlük Doğrulama Ekranı

- MySQL ile blockchain verilerinin gerçek zamanlı karşılaştırması
- Uyuşmazlık kategorileri: **Eşleşiyor / Genesis Eksik / DB Fazla / Blockchain Fazla**
- Renk kodlu tablo ve AJAX lot geçmişi modalı
- Olası veritabanı manipülasyonunu otomatik tespit eder

---

## 📁 Klasör Yapısı

```
NumuneStok_with_Blockchain/
├── Blockchain/
│   ├── contracts/
│   │   └── SupplyChainLedger.sol        # Akıllı sözleşme
│   ├── scripts/
│   │   ├── deploy.js                    # Deploy scripti
│   │   ├── measure_performance.js       # Gas & performans ölçümü
│   │   └── start_and_sync.sh           # Hardhat başlatma
│   ├── test/
│   │   └── SupplyChainLedger.test.js    # 13 akıllı sözleşme testi
│   ├── hardhat.config.js
│   └── package.json
├── NumuneStok/
│   ├── Controllers/
│   │   ├── ProductController.cs         # Stok ve tedarik zinciri
│   │   └── BlockchainExplorerController.cs # Bütünlük doğrulama
│   ├── Models/
│   │   ├── SupplyChainActor.cs          # Supplier, Carrier, Warehouse, Lab
│   │   ├── SupplyChainProcess.cs        # Shipment, Transfer, Receipt
│   │   ├── ChildProduct.cs             # Lot bazlı stok
│   │   └── User.cs                    # BlockchainRole, WalletAddress
│   ├── Services/
│   │   ├── IBlockchainService.cs        # Servis arayüzü
│   │   ├── BlockchainService.cs         # Nethereum implementasyonu
│   │   └── BlockchainStartupStockInitializer.cs
│   └── Views/
│       ├── Product/
│       │   ├── SupplyChainSimulation.cshtml
│       │   └── ProductBlockchainHistory.cshtml
│       └── BlockchainExplorer/
│           └── Index.cshtml
├── PERFORMANCE_RESULTS.md              # Ölçüm sonuçları (tez verisi)
├── INTEGRATION_TEST_SCENARIOS.md       # Test senaryoları
├── README.md       # Bu dosya
```

---

## ⛓️ Akıllı Sözleşme

### İşlem Tipleri (ActionType)

| Değer | İşlem           | Kim Yapabilir         |
| ----- | --------------- | --------------------- |
| 0     | Added           | Warehouse             |
| 1     | Deducted        | Warehouse, Laboratory |
| 2     | **Produced**    | Producer              |
| 3     | **Shipped**     | Producer              |
| 4     | **Received**    | Warehouse             |
| 5     | **Transferred** | Warehouse             |
| 6     | **Consumed**    | Laboratory            |
| 7     | **Genesis**     | Admin                 |

### İş Kuralları (On-Chain)

```solidity
// Produced olmadan Shipped yapılamaz
require(status.state == LotState.Produced, "Lot must be produced before shipment");

// Shipped olmadan Received yapılamaz
require(status.state == LotState.Shipped, "Lot must be shipped before receipt");

// Stok miktarı aşılamaz
require(status.onChainQuantity >= quantity, "Action exceeds on-chain stock");

// Genesis bir kez oluşturulabilir
require(!status.exists, "Genesis already exists for this lot");
```

---

## 📊 Performans Ölçümleri

Lokal Hardhat üzerinde 5 bağımsız çalışmanın ortalaması:

| İşlem                | Gas Kullanımı   | Ort. Süre  |
| -------------------- | --------------- | ---------- |
| Produced             | 262.001         | 1,8 ms     |
| Shipped              | 207.486         | 2,4 ms     |
| Received             | 227.990         | 1,8 ms     |
| Transferred          | 207.680         | 1,8 ms     |
| Consumed             | 210.192         | 1,8 ms     |
| **Batch Genesis ×3** | **743.129**     | **3,2 ms** |
| getHistory (okuma)   | **0 (gas yok)** | 4,2 ms     |

> Detaylı analiz ve geleneksel veritabanı karşılaştırması için → [`PERFORMANCE_RESULTS.md`](./PERFORMANCE_RESULTS.md)

---

## 🛠️ Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [MySQL 8](https://dev.mysql.com/downloads/)

### 1. Depoyu klonla

```bash
git clone https://github.com/kullanici-adi/NumuneStok_with_Blockchain.git
cd NumuneStok_with_Blockchain
```

### 2. MySQL bağlantısını yapılandır

`NumuneStok/appsettings.json` dosyasındaki connection string'i düzenle:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=numunestok;User=root;Password=SIFREN;"
  },
  "Blockchain": {
    "RpcUrl": "http://127.0.0.1:8545",
    "PrivateKey": "HARDHAT_PRIVATE_KEY",
    "ContractAddress": "DEPLOY_SONRASI_DOLDURULACAK"
  }
}
```

### 3. Blockchain paketlerini yükle

```bash
cd Blockchain
npm install
```

### 4. ASP.NET projesini derle

```bash
cd NumuneStok
dotnet build
```

### 5. Hardhat node'u başlat, sözleşmeyi deploy et ve stokları senkronize et

```bash
# Proje kökünden çalıştır
./Blockchain/scripts/start_and_sync.sh
```

Bu script sırasıyla şunları yapar:

1. Hardhat lokal node'u başlatır (`http://127.0.0.1:8545`)
2. `SupplyChainLedger` sözleşmesini deploy eder
3. Contract adresini `appsettings.json`'a yazar
4. ASP.NET uygulamasını başlatır
5. Başlangıç stokları yalnızca bu script tarafından blockchain'e Genesis kaydıyla yazılır

### 6. Uygulamayı ayrı çalıştırmak için

```bash
# Terminal 1 — Hardhat node
cd Blockchain && npx hardhat node

# Terminal 2 — Sözleşmeyi deploy et
cd Blockchain && npx hardhat run scripts/deploy.js --network localhost

# Terminal 3 — ASP.NET uygulaması
cd NumuneStok && dotnet run
```

Uygulama varsayılan olarak `https://localhost:5001` adresinde çalışır.

---

## 🧪 Testler

### Akıllı Sözleşme Birim Testleri

```bash
cd Blockchain
npx hardhat test
```

**13 test kapsamı:**

| Test                                           | Açıklama                                 |
| ---------------------------------------------- | ---------------------------------------- |
| ✅ records a complete lot journey              | Üretimden tüketime tam akış              |
| ✅ rejects shipment before production          | Sıra kuralı doğrulaması                  |
| ✅ rejects receipt before shipment             | Sıra kuralı doğrulaması                  |
| ✅ rejects transfer before receipt             | Depo teslimi olmadan transfer engeli     |
| ✅ rejects shipment quantity mismatch          | Üretilen ve sevk edilen miktar eşleşmesi |
| ✅ rejects receipt quantity mismatch           | Sevk ve teslim alınan miktar eşleşmesi   |
| ✅ rejects consumption that exceeds stock      | Stok aşımı koruması                      |
| ✅ rejects duplicate genesis records           | Genesis tekrar koruması                  |
| ✅ rejects unauthorized actors                 | Rol tabanlı erişim                       |
| ✅ rejects batch with mismatched lengths       | Batch veri bütünlüğü                     |
| ✅ deducts stock from a genesis lot            | Genesis + Deducted akışı                 |
| ✅ preserves remaining quantity after transfer | Kısmi tüketimde kalan stok               |
| ✅ marks lot consumed when all quantity gone   | Tam tüketim durumu                       |

### Performans Ölçümü

```bash
cd Blockchain
npm run measure
```

Gas kullanımı ve işlem sürelerini tablo olarak ekrana yazar.

---

## 🔑 Kullanıcı Rolleri

| Uygulama Rolü | Blockchain Rolü | Yetkili İşlemler                       |
| ------------- | --------------- | -------------------------------------- |
| Admin         | Admin           | Tümü                                   |
| SuperUser     | Admin           | Tümü                                   |
| Supplier      | Producer        | Produced, Shipped                      |
| Warehouse     | Warehouse       | Received, Transferred, Added, Deducted |
| Laboratory    | Laboratory      | Consumed, Deducted                     |

---

## 📺 Ekranlar

| Ekran                       | URL                                      | Açıklama                         |
| --------------------------- | ---------------------------------------- | -------------------------------- |
| Tedarik Zinciri Simülasyonu | `/Product/SupplyChainSimulation`         | Adım adım lot hareketleri        |
| Blockchain Geçmiş           | `/Product/BlockchainHistory?lotNumber=X` | Tekil lot geçmişi                |
| Ürün Blockchain Geçmişi     | `/Product/ProductBlockchainHistory/5`    | Ürün bazlı geçmiş + doğrulama    |
| **Bütünlük Doğrulama**      | `/BlockchainExplorer`                    | DB vs Blockchain karşılaştırması |
| Stok Yönetimi               | `/Product/ManageStock`                   | Stok listesi                     |
| Kritik Stok                 | `/Product/LowStock`                      | Kritik altı stoklar              |

---

## ⚠️ Sınırlılıklar

- **Tek cüzdan (Proxy İmzalama):** Tüm blockchain işlemleri tek bir Hardhat hesabıyla imzalanır. Gerçek dağıtık senaryoda her aktörün kendi private key'i kullanılmalıdır.
- **Lokal ağ:** Bu sistem public blockchain veya üretim ağında çalışmak için tasarlanmamıştır. Ethereum mainnet veya Hyperledger ağına geçiş için minimal uyarlama gerekir.
- **Gas maliyeti:** Lokal Hardhat'te gas ücreti yoktur; üretim ortamında gerçek maliyet hesaplanmalıdır.

---

## 📚 Kullanılan Teknolojiler

| Teknoloji                                                       | Amaç                         |
| --------------------------------------------------------------- | ---------------------------- |
| [ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc) | Web uygulama çatısı          |
| [Entity Framework Core](https://learn.microsoft.com/ef/core/)   | ORM                          |
| [Nethereum](https://nethereum.com/)                             | .NET ↔ Ethereum entegrasyonu |
| [Solidity](https://soliditylang.org/)                           | Akıllı sözleşme dili         |
| [Hardhat](https://hardhat.org/)                                 | EVM geliştirme ortamı        |
| [Chai](https://www.chaijs.com/)                                 | JavaScript test kütüphanesi  |
| [MySQL](https://www.mysql.com/)                                 | İlişkisel veritabanı         |

---

## 📄 Lisans

Bu proje akademik amaçlı geliştirilmiştir.

---

<div align="center">
<sub>Geliştirici: Özkan Kaya &nbsp;·&nbsp; Tez Yazarı: Yusuf Alper Yıldırım &nbsp;·&nbsp; Akademik Prototip &nbsp;·&nbsp; 2026</sub>
</div>
