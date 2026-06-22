# Entegrasyon Test Senaryoları — Blockchain Tabanlı Tedarik Zinciri Yönetimi

Tarih: 2026-06-22  
Test Ortamı: ASP.NET Core MVC + MySQL + Lokal Hardhat + SupplyChainLedger  
Blockchain Tipi: Lokal Hardhat üzerinde izinli/özel blockchain prototipi  
Otomatik Test Komutu: `cd Blockchain && npx hardhat test`

Bu doküman, projenin tez konusu olan **blockchain tabanlı tedarik zinciri yönetimi** kapsamını test etmek için hazırlanmıştır. Testler yalnızca akıllı sözleşmenin çalışmasını değil; kullanıcı rolleri, stok/lot kayıtları, MySQL ile blockchain tutarlılığı, ilk kurulum senkronizasyonu ve hata durumlarını da kapsar.

---

## 1. Test Profili

| Rol / Aktör | Projedeki karşılığı | Blockchain rolü | Testteki sorumluluk |
|---|---|---|---|
| Admin / SuperUser | Sistem yöneticisi | Admin | Aktör rolü tanımlar, ilk stok Genesis kaydını başlatır |
| Tedarikçi / Üretici | Supplier / producer account | Producer | Lot üretir ve sevkiyata çıkarır |
| Depo | Warehouse account | Warehouse | Sevkiyatı teslim alır, laboratuvara transfer eder |
| Laboratuvar | Laboratory account | Laboratory | Numune tüketimini zincire işler |
| Yetkisiz kullanıcı | Rol atanmamış kullanıcı | None | Zincir işlemi yapamaz |

**Test verisi ilkesi:** Her testte farklı lot numarası kullanılmalıdır. Önerilen biçim: `THESIS-SC-YYYYMMDD-001`. Böylece MySQL kayıtları ve blockchain geçmişi birbiriyle kolay eşleştirilir.

---

## 2. Otomatik Akıllı Sözleşme Testleri

Son çalıştırma sonucu:

```text
  SupplyChainLedger
    ✔ records a complete lot journey from production to consumption
    ✔ rejects shipment before production
    ✔ rejects receipt before shipment
    ✔ rejects transfer before the warehouse has received the shipment
    ✔ rejects shipment when shipped quantity differs from produced quantity
    ✔ rejects receipt when received quantity differs from shipped quantity
    ✔ rejects consumption that exceeds on-chain stock
    ✔ rejects duplicate genesis records for the same lot
    ✔ rejects actions from unauthorized actors
    ✔ rejects batch actions with mismatched array lengths
    ✔ deducts stock from a genesis lot and updates on-chain quantity
    ✔ allows consumption after warehouse transfer and preserves remaining quantity
    ✔ marks the lot consumed when all on-chain quantity is consumed

  13 passing (623ms)
```

### Otomatik Test Kapsamı

| Test | Senaryo | Beklenen güvence |
|---|---|---|
| T-01 | Üretim → sevkiyat → teslim alma → transfer → tüketim | Lot geçmişi kronolojik tutulur; kalan zincir stoku doğru hesaplanır |
| T-02 | Üretim olmadan sevkiyat | Sözleşme işlemi reddeder |
| T-03 | Sevkiyat olmadan teslim alma | Sözleşme işlemi reddeder |
| T-04 | Teslim alınmadan transfer | Sözleşme işlemi reddeder |
| T-05 | Üretilen miktardan farklı sevkiyat | Sözleşme miktar uyuşmazlığını reddeder |
| T-06 | Sevk edilen miktardan farklı teslim alma | Sözleşme miktar uyuşmazlığını reddeder |
| T-07 | Stoktan fazla tüketim | Zincir üzerindeki stok negatife düşmez |
| T-08 | Aynı lot için ikinci Genesis | Başlangıç stoğu tekrar yazılamaz |
| T-09 | Yetkisiz kullanıcı işlemi | Rol tabanlı erişim sözleşme seviyesinde korunur |
| T-10 | Batch array uzunluk uyumsuzluğu | Toplu Genesis veri bütünlüğü korunur |
| T-11 | Genesis sonrası stok düşme | Mevcut stoktan düşüm zincirde izlenir |
| T-12 | Transfer sonrası kısmi tüketim | Kalan stok korunur, lot tamamen kapanmaz |
| T-13 | Tam tüketim | Lot durumu `Consumed`, miktar `0` olur |

---

## 3. Uygulama Entegrasyon Testleri

Bu bölümdeki testler, uygulama çalışırken yapılmalıdır. Amaç, ASP.NET Core ekranları, MySQL kayıtları ve Hardhat üzerindeki sözleşme durumunun birlikte doğrulanmasıdır.

### IT-01 — İlk Kurulumda Script Kontrollü Blockchain Senkronizasyonu

| Alan | Detay |
|---|---|
| Ön koşul | MySQL içinde en az bir ürün ve ChildProduct lot kaydı bulunur; Hardhat node çalışır |
| Adımlar | `Blockchain/scripts/start_and_sync.sh` ile sistem başlatılır |
| Beklenen DB sonucu | MySQL stok kayıtları değişmeden kalır |
| Beklenen blockchain sonucu | Her mevcut lot için tek bir `Genesis` kaydı oluşur |
| Kanıt | `ProductBlockchainHistory` ekranında Genesis kaydı ve `getLotStatus()` içinde `exists=true` |

**Negatif kontrol:** Uygulama doğrudan `--sync-blockchain` parametresiyle çalıştırıldığında senkronizasyon reddedilmelidir. Ürün listesinde ayrıca manuel "Blockchain Sync" butonu bulunmamalıdır.

### IT-02 — Yeni Lotun Uçtan Uca Tedarik Zinciri Akışı

| Alan | Detay |
|---|---|
| Ön koşul | Admin giriş yapmış, tedarikçi/depo/laboratuvar rolleri tanımlı, yeni lot seçilmiş |
| Adımlar | Üret → Sevket → Depoda Teslim Al → Laboratuvara Transfer Et → Tüket |
| Beklenen DB sonucu | Sevkiyat, teslim alma ve transfer kayıtları ilgili tedarik zinciri tablolarına yazılır; stok miktarı tüketim kadar azalır |
| Beklenen blockchain sonucu | Lot geçmişinde 5 kayıt bulunur: Produced, Shipped, Received, Transferred, Consumed |
| Kanıt | Her adımda transaction hash görünür; `/Product/BlockchainHistory?lotNumber=...` kronolojik kayıt gösterir |

### IT-03 — Kısmi Tüketimde Kalan Stok Tutarlılığı

| Alan | Detay |
|---|---|
| Ön koşul | 20 adetlik lot depoda teslim alınmış ve laboratuvara transfer edilmiş |
| Adımlar | Laboratuvar 6 adet tüketir |
| Beklenen DB sonucu | ChildProduct miktarı 14 olarak kalır |
| Beklenen blockchain sonucu | `onChainQuantity=14`, lot state `Transferred` veya stokta kullanılabilir durumda kalır |
| Kanıt | Blockchain Explorer ekranında DB stok ve zincir stoku eşleşir |

### IT-04 — Tam Tüketimde Lot Kapanışı

| Alan | Detay |
|---|---|
| Ön koşul | 8 adetlik lot laboratuvara transfer edilmiş |
| Adımlar | Laboratuvar 8 adet tüketir |
| Beklenen DB sonucu | İlgili lotun miktarı 0 olur veya stoktan kaldırılmış kabul edilir |
| Beklenen blockchain sonucu | `onChainQuantity=0`, lot state `Consumed` |
| Kanıt | Lot geçmişinde son kayıt `Consumed`; explorer fark göstermemelidir |

### IT-05 — Blockchain Kapalıyken İşlem Reddetme

| Alan | Detay |
|---|---|
| Ön koşul | Uygulama çalışır, Hardhat node durdurulur |
| Adımlar | Supply Chain Simulation ekranında üretim veya tüketim adımı denenir |
| Beklenen DB sonucu | İşlem başarısız olduğu için DB stok ve süreç tablolarında yeni kayıt oluşmaz |
| Beklenen blockchain sonucu | Transaction hash oluşmaz |
| Kanıt | Kullanıcıya blockchain bağlantı hatası gösterilir; ilgili lot geçmişi değişmez |

### IT-06 — Veritabanı Manipülasyonu Tespiti

| Alan | Detay |
|---|---|
| Ön koşul | Aynı lot hem MySQL'de hem blockchain'de kayıtlıdır |
| Adımlar | MySQL'de lot miktarı doğrudan artırılır: `UPDATE ChildProducts SET Quantity = Quantity + 50 WHERE LotNumber = 'HEDEF_LOT';` |
| Beklenen DB sonucu | DB miktarı blockchain miktarından büyük görünür |
| Beklenen blockchain sonucu | Zincirdeki miktar değişmez |
| Kanıt | `/BlockchainExplorer` ekranı ilgili lot için `DB Fazla` veya tutarsızlık uyarısı verir |

### IT-07 — Genesis Eksik Lot Tespiti

| Alan | Detay |
|---|---|
| Ön koşul | MySQL'de var olan bir lot yeni deploy edilen kontrata henüz yazılmamıştır |
| Adımlar | Explorer ekranı açılır |
| Beklenen DB sonucu | Lot MySQL'de görünür |
| Beklenen blockchain sonucu | `getLotStatus()` için `exists=false` |
| Kanıt | Explorer üzerinde `Genesis Eksik` uyarısı görünür |

### IT-08 — Yetkisiz Rol ile Zincir İşlemi Denemesi

| Alan | Detay |
|---|---|
| Ön koşul | BlockchainRole değeri boş veya `None` olan kullanıcıyla giriş yapılır |
| Adımlar | Supply Chain Simulation adımı tetiklenmeye çalışılır |
| Beklenen DB sonucu | Yetkisiz işlem için süreç kaydı oluşmaz |
| Beklenen blockchain sonucu | Sözleşme `Actor is not authorized for this action` ile reddeder |
| Kanıt | Kullanıcı hata alır; lot geçmişi değişmez |

### IT-09 — Yanlış Miktar Girişi

| Alan | Detay |
|---|---|
| Ön koşul | 10 adet üretilmiş lot sevkiyat aşamasındadır |
| Adımlar | 8 adet sevk veya 9 adet teslim alma denenir |
| Beklenen DB sonucu | Hatalı miktar uygulama tarafından engellenir; engellenmezse sözleşme reddeder ve DB geri alınır |
| Beklenen blockchain sonucu | `Shipment quantity must match produced quantity` veya `Receipt quantity must match shipped quantity` hatası |
| Kanıt | Transaction hash oluşmaz; lot geçmişindeki kayıt sayısı değişmez |

### IT-10 — Başlangıç Senkronizasyonunun Tekrarlanamaması

| Alan | Detay |
|---|---|
| Ön koşul | Bir lot için Genesis daha önce yazılmıştır |
| Adımlar | Aynı lot için başlangıç senkronizasyonu tekrar tetiklenir |
| Beklenen DB sonucu | DB miktarı değişmez |
| Beklenen blockchain sonucu | İkinci Genesis `Genesis already exists for this lot` ile reddedilir veya uygulama lotu atlar |
| Kanıt | Lot geçmişinde yalnızca bir Genesis kaydı vardır |

---

## 4. Test Sonuç Kayıt Şablonu

| Test | Test türü | Durum | Kanıt |
|---|---|---|---|
| T-01 - T-13 | Otomatik Hardhat | Geçti | `13 passing (623ms)` |
| IT-01 | Uygulama entegrasyon | Yapılacak | Script çıktısı + Genesis geçmişi |
| IT-02 | Uygulama entegrasyon | Yapılacak | Transaction hash listesi + DB kayıtları |
| IT-03 | Uygulama entegrasyon | Yapılacak | Explorer stok eşleşmesi |
| IT-04 | Uygulama entegrasyon | Yapılacak | `Consumed` lot durumu |
| IT-05 | Hata dayanıklılığı | Yapılacak | Bağlantı hatası + değişmeyen DB |
| IT-06 | Bütünlük doğrulama | Yapılacak | Explorer tutarsızlık uyarısı |
| IT-07 | Eksik Genesis tespiti | Yapılacak | `exists=false` / uyarı rozeti |
| IT-08 | Yetki kontrolü | Yapılacak | Yetkisiz işlem hatası |
| IT-09 | Miktar doğrulama | Yapılacak | Reddedilen işlem |
| IT-10 | Tekrarlı sync koruması | Yapılacak | Tek Genesis kaydı |

---

## 5. Tez İçin Değerlendirme

Bu test seti, yalnızca kontrat fonksiyonlarının çalıştığını değil; tedarik zinciri yönetimi açısından önemli olan dört temel iddiayı doğrular:

1. Lot hareketleri değiştirilemez blockchain geçmişi olarak tutulur.
2. İş sırası ve miktar kuralları akıllı sözleşme seviyesinde zorlanır.
3. MySQL ile blockchain arasındaki stok farkı tespit edilebilir.
4. İlk veri aktarımı kontrollü script ile yapılır; kullanıcı arayüzünden keyfi sync erişimi yoktur.

Bu nedenle test kapsamı, lokal Hardhat kullanılmasına rağmen tez prototipi için gerçekçi bir tedarik zinciri doğrulama zemini sağlar.
