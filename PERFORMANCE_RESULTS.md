# Performans Ölçüm Raporu — SupplyChainLedger (Lokal Hardhat)

Tarih: 2026-06-22  
Ölçüm Komutu: `cd Blockchain && npm run measure`  
Ölçüm Scripti: `Blockchain/scripts/measure_performance.js`

---

## 1. Ortam

| Parametre | Değer |
|---|---|
| Blockchain ağı | Lokal Hardhat |
| Ağ tipi | İzinli/özel blockchain prototipi |
| Solidity sürümü | 0.8.24 |
| Hardhat sürümü | ^2.22.0 |
| Ölçüm tekrar sayısı | 5 bağımsız çalışma |
| Test edilen sözleşme | `SupplyChainLedger` |

> Not: Lokal Hardhat ölçümleri gerçek bir genel ağ gecikmesini içermez. Bu rapor, sözleşme gas maliyetini ve geliştirme ortamındaki işlem sürelerini göstermek için kullanılır. Gerçek ağda blok süresi, RPC gecikmesi ve yoğunluk ayrıca ölçülmelidir.

---

## 2. Tekil İşlem Sonuçları

| İşlem | ActionType | Gas kullanımı | Ortalama süre | Min | Max |
|---|---:|---:|---:|---:|---:|
| Produced | 2 | 262.001 | 1,8 ms | 1 ms | 2 ms |
| Shipped | 3 | 207.486 | 2,4 ms | 1 ms | 6 ms |
| Received | 4 | 227.990 | 1,8 ms | 1 ms | 2 ms |
| Transferred | 5 | 207.680 | 1,8 ms | 1 ms | 2 ms |
| Consumed | 6 | 210.192 | 1,8 ms | 1 ms | 2 ms |
| **Toplam tekil akış** |  | **1.115.349** | **9,6 ms** |  |  |

### Yorum

- En yüksek gas maliyeti `Produced` işlemindedir. Çünkü lot ilk kez aktif sürece girer ve sözleşme `LotStatus` alanlarını başlatır.
- En düşük gas maliyetleri `Shipped` ve `Transferred` işlemlerindedir. Bu adımlar yeni lot stoğu yaratmadan mevcut durum alanlarını günceller.
- `Received` işlemi, sevk edilen miktarı zincir üzerindeki stok miktarına eklediği için `Shipped` işleminden daha pahalıdır.
- Gas değerleri 5 çalışmada da aynı kalmıştır. Bu beklenen bir sonuçtur; aynı sözleşme kodu ve aynı giriş verileri EVM üzerinde aynı gas yolunu üretir.
- Süre değerleri milisaniye düzeyindedir ve lokal makine yüküne duyarlıdır. Bu nedenle tezde ana performans göstergesi olarak gas maliyeti daha güvenilir kabul edilmelidir.

---

## 3. Toplu Genesis Ölçümü

`logActions()` fonksiyonu, birden fazla lotun başlangıç kaydını tek transaction içinde yazar.

| İşlem | Lot sayısı | Gas kullanımı | Ortalama süre | Lot başına gas |
|---|---:|---:|---:|---:|
| Batch Genesis x3 | 3 | 743.129 | 3,2 ms | ~247.710 |

### Batch ve Tekil Karşılaştırma

| Ölçüm | 3 ayrı Produced/Genesis benzeri yazım | Batch Genesis x3 | Fark |
|---|---:|---:|---:|
| Toplam gas | ~786.003 | 743.129 | ~42.874 gas daha düşük |
| Transaction sayısı | 3 | 1 | 2 transaction daha az |
| Yaklaşık tasarruf |  |  | %5,5 |

Bu karşılaştırma yaklaşık referanstır; ölçüm scripti tekil Genesis işlemini ayrıca raporlamadığı için ilk zincir yazımı maliyetine en yakın tekil işlem olan `Produced` değeri baz alınmıştır.

**Tez yorumu:** İlk kurulumda çok sayıda mevcut stok lotu blockchain'e yazılacağı için batch kullanımının değeri yüksektir. Lokal Hardhat'te parasal maliyet oluşmasa da, gas azalması gerçek bir izinli ağda kaynak tüketimini azaltacak şekilde yorumlanabilir.

---

## 4. Okuma İşlemleri

| İşlem | Gas | Ortalama süre | Min | Max |
|---|---:|---:|---:|---:|
| `getHistory()` — 5 kayıtlık lot | 0 | 4,2 ms | 3 ms | 7 ms |
| `getLotStatus()` | 0 | < 1 ms | < 1 ms | < 1 ms |

`view` fonksiyonları transaction üretmediği için gas harcamaz. Bu yüzden ürün geçmişi ve lot durum ekranları blockchain'e yazma maliyeti oluşturmadan denetim izi sunabilir.

---

## 5. Ham Ölçüm Verileri

| Çalışma | Produced | Shipped | Received | Transferred | Consumed | Batch x3 | History read |
|---|---:|---:|---:|---:|---:|---:|---:|
| Run 1 | 2 ms | 1 ms | 1 ms | 1 ms | 1 ms | 2 ms | 3 ms |
| Run 2 | 2 ms | 2 ms | 2 ms | 2 ms | 2 ms | 4 ms | 7 ms |
| Run 3 | 1 ms | 6 ms | 2 ms | 2 ms | 2 ms | 4 ms | 5 ms |
| Run 4 | 2 ms | 2 ms | 2 ms | 2 ms | 2 ms | 3 ms | 3 ms |
| Run 5 | 2 ms | 1 ms | 2 ms | 2 ms | 2 ms | 3 ms | 3 ms |
| **Ortalama** | **1,8 ms** | **2,4 ms** | **1,8 ms** | **1,8 ms** | **1,8 ms** | **3,2 ms** | **4,2 ms** |

Sabit gas değerleri:

| İşlem | Gas |
|---|---:|
| Produced | 262.001 |
| Shipped | 207.486 |
| Received | 227.990 |
| Transferred | 207.680 |
| Consumed | 210.192 |
| Batch Genesis x3 | 743.129 |

---

## 6. Geleneksel MySQL ile Karşılaştırma

| Kriter | Sadece MySQL | Blockchain + MySQL yaklaşımı |
|---|---|---|
| Yazma hızı | Lokal ortamda çok hızlı | Lokal Hardhat'te milisaniye düzeyinde |
| Değiştirilebilirlik | `UPDATE` / `DELETE` ile değiştirilebilir | Blockchain geçmişi değiştirilemez |
| Denetim izi | Ek log/trigger tasarımı gerekir | Her işlem sözleşme geçmişinde tutulur |
| İş kuralı koruması | Uygulama katmanına bağlıdır | Sözleşme seviyesinde zorlanır |
| Yetki kontrolü | Uygulama/DB seviyesinde | Sözleşme rol kontrolüyle desteklenir |
| Tutarsızlık tespiti | Manuel sorgu gerekir | Explorer ekranı DB-zincir farkını gösterebilir |

**Sonuç:** Bu prototipte blockchain kullanımı performans avantajı için değil, tedarik zinciri kayıtlarının değiştirilemezliği, izlenebilirliği ve iş kurallarının merkezi uygulama kodu dışında da doğrulanması için anlamlıdır. Lokal Hardhat ağı, tez kapsamında gerçek ağa çıkmadan bu mimari iddiayı ölçülebilir şekilde göstermeye yeterlidir.
