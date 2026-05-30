using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;
using NumuneStok.Services;
using System.Linq.Expressions;

namespace NumuneStok.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlockchainService _blockchainService;

        public ProductController(ApplicationDbContext context, IBlockchainService blockchainService)
        {
            _context = context;
            _blockchainService = blockchainService;
        }

        // GET: Product
        //public async Task<IActionResult> Index()
        //{
        //    var products = await _context.Products.Include(p => p.Category).ToListAsync();
        //    return View(products);
        //}
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.ChildProducts) // Include child products
                                         .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> OrderList()
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.ChildProducts)
                                         .ToListAsync();

            // Eksik stok miktarını hesaplayalım ve modelin `RequiredOrder` özelliğini hesaplayalım
            var orderList = products
                            .Where(p => p.Order.HasValue && p.Order.Value > p.TotalQuantity)
                            .Select(p => new Product
                            {
                                Id = p.Id,
                                ProductName = p.ProductName,
                                ReferenceNumber = p.ReferenceNumber,
                                Category = p.Category,
                                Order = p.Order,
                                Quantity = p.Quantity,
                                ChildProducts = p.ChildProducts,
                            })
                            .ToList();

            return View(orderList);
        }
        public async Task<IActionResult> LowStock()
        {
            // Önce veriyi çekiyoruz
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ChildProducts)
                .ToListAsync();  // **Önce veriyi belleğe alıyoruz**

            // Bellekte filtreleme yapıyoruz
            var lowStockProducts = products
                .Where(p => p.TotalQuantity <= p.Critical.GetValueOrDefault(0))
                .ToList();

            return View(lowStockProducts);
        }



        public async Task<IActionResult> VisitorProduct(string referenceNo)
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.ChildProducts)
                                         .ToListAsync();

            // Eğer referenceNo varsa, listeyi filtrele
            if (!string.IsNullOrEmpty(referenceNo))
            {
                products = products.Where(p => p.ReferenceNumber == referenceNo).ToList();
            }

            return View(products);
        }

        // GET: AddChildProduct
        public IActionResult BarcodeAdd()
        {
            return View();
        }

        public async Task<IActionResult> BarcodeStockAdd(string referenceNo, string lotNumber, string expirationDate)
        {
            // Ürünü bulma
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNo);
            if (product != null)
            {
                var childProduct = new ChildProduct
                {
                    ProductId = product.Id,
                    LotNumber = lotNumber,
                    ExpirationDate = DateTime.Parse(expirationDate),
                    Quantity = 0 // Kullanıcıdan alacak
                };

                return View(childProduct); // Formda doldurulacak değerlerle yeni view döneriz
            }

            ModelState.AddModelError("", "Ürün bulunamadı.");
            return View(new ChildProduct());
        }


        // GET: AddChildProduct
        public IActionResult BarcodeDeduct()
        {
            return View();
        }

        public async Task<IActionResult> ProductsByCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                                         .Where(p => p.CategoryId == id)
                                         .Include(p => p.Category)
                                         .Include(p => p.ChildProducts)
                                         .ToListAsync();

            ViewData["CategoryName"] = category.CategoryName; // Kategori adını ViewData ile gönderiyoruz
            return View(products);
        }



        // GET: Product/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName");
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            //if (ModelState.IsValid)
            //{
                // Kullanıcıya görünmeyecek alanlara varsayılan değerleri atıyoruz
                product.Location = "NUMUNE SOĞUK HAVA";
                product.Quantity = 0;
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}
            //ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName", product.CategoryId);
            //return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}
            //ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName", product.CategoryId);
            //return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // Action for managing child products (ManageStock)
        public async Task<IActionResult> ManageStock(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        public async Task<IActionResult> ExpiringStock()
        {
            // Tüm child product'ları veritabanından çekiyoruz ve AsEnumerable ile belleğe alıyoruz.
            var expiringChildProducts = _context.ChildProducts
                .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
                .AsEnumerable() // SQL sorgusundan sonra bellek işlemleri için AsEnumerable() kullanıyoruz
                .Where(cp => (cp.ExpirationDate - DateTime.Now).TotalDays <= 30) // Bellek üzerinde filtreleme yapıyoruz
                .ToList(); // Listeye çeviriyoruz

            return View(expiringChildProducts);
        }


        // GET: AddChildProduct
        public async Task<IActionResult> AddChildProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.ProductName; // Ürün adını ViewBag'e ekle
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();
            return View();
        }

        // POST: AddChildProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChildProduct(ChildProduct childProduct)
        {
            if (!await _blockchainService.IsBlockchainAvailableAsync())
            {
                TempData["Error"] = "Blockchain ağı şu anda erişilemez durumda. İşlem güvenlik nedeniyle durduruldu.";
                return RedirectToAction(nameof(Index));
            }

            //if (ModelState.IsValid)
            //{


                // ExpirationDate alanını dd.MM.yyyy formatından DateTime'e çeviriyoruz
                var expirationDateString = Request.Form["ExpirationDate"];

                if (DateTime.TryParseExact(expirationDateString, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime expirationDate))
                {
                    childProduct.ExpirationDate = expirationDate;
                }
                else
                {
                    // Hatalı tarih formatı durumunda hata ekleyip formu geri döndürme
                    ModelState.AddModelError("ExpirationDate", "Geçersiz tarih formatı.");
                    ViewBag.ProductId = childProduct.ProductId;
                    return View(childProduct);
                }


                // Üretim tarihine varsayılan bir değer atıyoruz
                childProduct.ProductionDate = new DateTime(2020, 1, 1);
                _context.ChildProducts.Add(childProduct);
                await _context.SaveChangesAsync();

                // Blockchain'e "Added" kaydı gönder
                await _blockchainService.LogActionAsync(childProduct.LotNumber, 0, childProduct.Quantity);

                return RedirectToAction(nameof(Index));

            //return RedirectToAction(nameof(ManageStock), new { id = childProduct.ProductId });


            //}
            //ViewBag.ProductId = childProduct.ProductId;
            //return View(childProduct);
        }

        public async Task<IActionResult> AddChildProductDirectly(string referenceNo, string lotNumber, string expirationDate)
        {
            var product = await _context.Products
                                                .Include(p => p.ChildProducts) // Eğer ChildProduct'lara erişmek gerekiyorsa
                                                .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNo);
            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";
                return RedirectToAction("Index");
            }

            // Toplam stok bilgisini al
            var totalQuantity = product.TotalQuantity;

            // Pass data to the view for the form
            ViewBag.ProductId = product.Id;
            ViewBag.ProductName = product.ProductName;
            ViewBag.LotNumber = lotNumber;
            ViewBag.ExpirationDate = expirationDate;
            ViewBag.TotalQuantity = totalQuantity; // Stok bilgisi
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();

            return View("AddChildProductDirectly"); // Use a new view here
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveChildProductDirectly(ChildProduct childProduct)
        {
                if (!await _blockchainService.IsBlockchainAvailableAsync())
                {
                    TempData["Error"] = "Blockchain ağı şu anda erişilemez durumda. İşlem güvenlik nedeniyle durduruldu.";
                    return RedirectToAction("Index", "Home");
                }
            
                // Attempt to parse ExpirationDate if needed
                if (DateTime.TryParseExact(Request.Form["ExpirationDate"], "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime expirationDate))
                {
                    childProduct.ExpirationDate = expirationDate;
                }

                childProduct.ProductionDate = DateTime.Now; // or any default value
                _context.ChildProducts.Add(childProduct);
                await _context.SaveChangesAsync();

                // Blockchain'e "Added" kaydı gönder
                await _blockchainService.LogActionAsync(childProduct.LotNumber, 0, childProduct.Quantity);

                return RedirectToAction("Index", "Home");
        }



        // GET: Product/StockDeduction/5
        public async Task<IActionResult> StockDeduction(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Alt ürünleri son kullanma tarihine göre sıralıyoruz.
            product.ChildProducts = product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList();
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();

            return View(product);
        }

        // POST: Product/StockDeduction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockDeduction(int productId, int quantityToDeduct)
        {
            if (!await _blockchainService.IsBlockchainAvailableAsync())
            {
                TempData["Error"] = "Blockchain ağı şu anda erişilemez durumda. Stok düşme işlemi iptal edildi.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            var deductedLots = new Dictionary<string, int>();

            // Stok düşme işlemi: son kullanma tarihine göre en yakın alt üründen başlayarak stok düşelim
            foreach (var childProduct in product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList())
            {
                if (quantityToDeduct <= 0)
                {
                    break;
                }

                int deducted = 0;
                if (childProduct.Quantity >= quantityToDeduct)
                {
                    // Bu alt ürün yeterli stoka sahipse, miktarını azaltıyoruz.
                    deducted = quantityToDeduct;
                    childProduct.Quantity -= quantityToDeduct;
                    quantityToDeduct = 0;

                    // Eğer stok sıfıra düştüyse, alt ürünü sil.
                    if (childProduct.Quantity == 0)
                    {
                        _context.ChildProducts.Remove(childProduct);
                    }
                }
                else
                {
                    // Stok yetersizse, bu alt ürünü tamamen sıfırla ve sil
                    deducted = childProduct.Quantity;
                    quantityToDeduct -= childProduct.Quantity;
                    _context.ChildProducts.Remove(childProduct);
                }

                if (deducted > 0)
                {
                    if (deductedLots.ContainsKey(childProduct.LotNumber))
                        deductedLots[childProduct.LotNumber] += deducted;
                    else
                        deductedLots[childProduct.LotNumber] = deducted;
                }
            }

            // Değişiklikleri kaydediyoruz
            await _context.SaveChangesAsync();

            // Blockchain'e "Deducted" kaydı gönder (etkilenen her lot için)
            foreach (var kvp in deductedLots)
            {
                await _blockchainService.LogActionAsync(kvp.Key, 1, kvp.Value);
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> StockDeductionBarcode(string referenceNo)
        {
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNo);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";
                return RedirectToAction("Index");
            }

            // Sort child products by expiration date
            product.ChildProducts = product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList();
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();

            return View("StockDeductionBarcode", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformBarcodeStockDeduction(int productId, int quantityToDeduct)
        {
            if (!await _blockchainService.IsBlockchainAvailableAsync())
            {
                TempData["Error"] = "Blockchain ağı şu anda erişilemez durumda. Stok düşme işlemi iptal edildi.";
                return RedirectToAction("Index", "Home");
            }

            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            var deductedLots = new Dictionary<string, int>();

            foreach (var childProduct in product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList())
            {
                if (quantityToDeduct <= 0)
                {
                    break;
                }

                int deducted = 0;
                if (childProduct.Quantity >= quantityToDeduct)
                {
                    deducted = quantityToDeduct;
                    childProduct.Quantity -= quantityToDeduct;
                    quantityToDeduct = 0;

                    if (childProduct.Quantity == 0)
                    {
                        _context.ChildProducts.Remove(childProduct);
                    }
                }
                else
                {
                    deducted = childProduct.Quantity;
                    quantityToDeduct -= childProduct.Quantity;
                    _context.ChildProducts.Remove(childProduct);
                }

                if (deducted > 0)
                {
                    if (deductedLots.ContainsKey(childProduct.LotNumber))
                        deductedLots[childProduct.LotNumber] += deducted;
                    else
                        deductedLots[childProduct.LotNumber] = deducted;
                }
            }

            await _context.SaveChangesAsync();

            // Blockchain'e "Deducted" kaydı gönder (etkilenen her lot için)
            foreach (var kvp in deductedLots)
            {
                await _blockchainService.LogActionAsync(kvp.Key, 1, kvp.Value);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Product/GetProductIdByReferenceNo
        public async Task<IActionResult> GetProductIdByReferenceNo(string referenceNo)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNo);
            if (product == null)
            {
                return Json(new { productId = (int?)null });
            }

            // Toplam stoku hesapla
            var totalStock = product.ChildProducts?.Sum(cp => cp.Quantity) ?? 0;

            return Json(new { 
                productId = product.Id,
                productName = product.ProductName,
                categoryName = product.Category?.CategoryName ?? "Kategori Yok",
                totalStock = totalStock,
                referenceNumber = product.ReferenceNumber
            });
        }


        // GET: Product/VisStockDeduction/5
        public async Task<IActionResult> VisStockDeduction(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Alt ürünleri son kullanma tarihine göre sıralıyoruz.
            product.ChildProducts = product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList();
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();

            return View(product);
        }

        // POST: Product/VisStockDeduction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VisStockDeduction(int productId, int quantityToDeduct = 1)
        {
            if (!await _blockchainService.IsBlockchainAvailableAsync())
            {
                TempData["Error"] = "Blockchain ağı şu anda erişilemez durumda. Stok düşme işlemi iptal edildi.";
                return RedirectToAction("BarcodeStockDeduction", "Product");
            }

            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            var deductedLots = new Dictionary<string, int>();

            // Stok düşme işlemi: son kullanma tarihine göre en yakın alt üründen başlayarak stok düşelim
            foreach (var childProduct in product.ChildProducts.OrderBy(cp => cp.ExpirationDate).ToList())
            {
                if (quantityToDeduct <= 0)
                {
                    break;
                }

                int deducted = 0;
                if (childProduct.Quantity >= quantityToDeduct)
                {
                    // Bu alt ürün yeterli stoka sahipse, miktarını azaltıyoruz.
                    deducted = quantityToDeduct;
                    childProduct.Quantity -= quantityToDeduct;
                    quantityToDeduct = 0;

                    // Eğer stok sıfıra düştüyse, alt ürünü sil.
                    if (childProduct.Quantity == 0)
                    {
                        _context.ChildProducts.Remove(childProduct);
                    }
                }
                else
                {
                    // Stok yetersizse, bu alt ürünü tamamen sıfırla ve sil
                    deducted = childProduct.Quantity;
                    quantityToDeduct -= childProduct.Quantity;
                    _context.ChildProducts.Remove(childProduct);
                }

                if (deducted > 0)
                {
                    if (deductedLots.ContainsKey(childProduct.LotNumber))
                        deductedLots[childProduct.LotNumber] += deducted;
                    else
                        deductedLots[childProduct.LotNumber] = deducted;
                }
            }

            // Değişiklikleri kaydediyoruz
            await _context.SaveChangesAsync();

            // Blockchain'e "Deducted" kaydı gönder (etkilenen her lot için)
            foreach (var kvp in deductedLots)
            {
                await _blockchainService.LogActionAsync(kvp.Key, 1, kvp.Value);
            }

            // TempData'ya bilgilendirme mesajını ekliyoruz
            TempData["SuccessMessage"] = $"{product.ProductName} adlı üründen 1 adet stok başarıyla düşürülmüştür.";

            return RedirectToAction("BarcodeStockDeduction", "Product");
        }

        // GET: Product/ProductTestRequirement
        public async Task<IActionResult> ProductTestRequirement()
        {
            var products = await _context.Products
                .Include(p => p.ChildProducts)
                .ToListAsync();

            return View(products);
        }


        public IActionResult BarcodeStockDeduction()
        {
            return View();
        }



        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // GET: Product/SyncBlockchain
        public async Task<IActionResult> SyncBlockchain()
        {
            var childProducts = await _context.ChildProducts.ToListAsync();
            int syncedCount = 0;

            foreach (var cp in childProducts)
            {
                if (cp.Quantity > 0)
                {
                    await _blockchainService.LogActionAsync(cp.LotNumber, 0, cp.Quantity);
                    syncedCount++;
                }
            }

            TempData["SuccessMessage"] = $"Sistemdeki mevcut stoklar başarıyla blokzincirine aktarıldı ({syncedCount} parti sekronize edildi).";
            return RedirectToAction("Index");
        }

        // GET: Product/BlockchainHistory?lotNumber=XXX
        public async Task<IActionResult> BlockchainHistory(string lotNumber)
        {
            if (string.IsNullOrEmpty(lotNumber))
            {
                return BadRequest("Lot numarası belirtilmedi.");
            }

            var records = await _blockchainService.GetHistoryAsync(lotNumber);

            ViewBag.LotNumber = lotNumber;
            return View(records);
        }

        // GET: Product/ProductBlockchainHistory/5
        public async Task<IActionResult> ProductBlockchainHistory(int id)
        {
            var product = await _context.Products
                .Include(p => p.ChildProducts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var allRecords = new List<BlockchainRecord>();
            bool isBcUp = await _blockchainService.IsBlockchainAvailableAsync();
            ViewBag.BlockchainAvailable = isBcUp;

            if (isBcUp)
            {
                var distinctLots = product.ChildProducts.Select(c => c.LotNumber).Distinct();

                foreach (var lot in distinctLots)
                {
                    var records = await _blockchainService.GetHistoryAsync(lot);
                    if (records != null)
                    {
                        allRecords.AddRange(records);
                    }
                }

                allRecords = allRecords.OrderByDescending(r => r.Timestamp).ToList();
            }

            ViewBag.ProductName = product.ProductName;
            ViewBag.DatabaseTotalStock = product.ChildProducts.Sum(c => c.Quantity);
            return View(allRecords);
        }
    }
}
