using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;
using NumuneStok.Services;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace NumuneStok.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlockchainService _blockchainService;

        public ProductController(
            ApplicationDbContext context,
            IBlockchainService blockchainService)
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
        [Authorize(Roles = "Admin,SuperUser")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName");
            return View();
        }

        // POST: Product/Create
        [Authorize(Roles = "Admin,SuperUser")]
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
        [Authorize(Roles = "Admin,SuperUser")]
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
        [Authorize(Roles = "Admin,SuperUser")]
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
        [Authorize(Roles = "Admin,SuperUser")]
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
        [Authorize(Roles = "Admin,SuperUser")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

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


        // GET: AddChildProduct — Artık simülasyona yönlendirir
        [Authorize(Roles = "Admin,SuperUser")]
        public async Task<IActionResult> AddChildProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.ProductName;
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();
            ViewBag.RedirectToSimulation = true; // Simülasyona yönlendirme bayrağı
            return View();
        }

        // POST: AddChildProduct — Tedarik zinciri bütünlüğü için simülasyona yönlendirir
        [Authorize(Roles = "Admin,SuperUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChildProduct(ChildProduct childProduct)
        {
            TempData["Info"] = "Stok girişi artık Tedarik Zinciri Simülasyonu üzerinden yapılmaktadır. Lütfen aşağıdaki sihirbazı kullanarak ürünü ekleyin.";
            return RedirectToAction(nameof(SupplyChainSimulation));
        }

        [Authorize(Roles = "Admin,SuperUser")]
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

        // POST: SaveChildProductDirectly — Tedarik zinciri bütünlüğü için simülasyona yönlendirir
        [Authorize(Roles = "Admin,SuperUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveChildProductDirectly(ChildProduct childProduct)
        {
            TempData["Info"] = "Stok girişi artık Tedarik Zinciri Simülasyonu üzerinden yapılmaktadır. Lütfen aşağıdaki sihirbazı kullanarak ürünü ekleyin.";
            return RedirectToAction(nameof(SupplyChainSimulation));
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
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            try
            {
                await SealAndApplyStockDeductionAsync(product, quantityToDeduct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Stok düşme işlemi iptal edildi: {ex.Message}";
                return RedirectToAction(nameof(Index));
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
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            try
            {
                await SealAndApplyStockDeductionAsync(product, quantityToDeduct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Stok düşme işlemi iptal edildi: {ex.Message}";
                return RedirectToAction("Index", "Home");
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
            var product = await _context.Products
                                        .Include(p => p.ChildProducts)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.ChildProducts == null || product.ChildProducts.Count == 0)
            {
                return NotFound("Ürün veya alt ürünler bulunamadı.");
            }

            try
            {
                await SealAndApplyStockDeductionAsync(product, quantityToDeduct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Stok düşme işlemi iptal edildi: {ex.Message}";
                return RedirectToAction("BarcodeStockDeduction", "Product");
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

        private async Task<string> SealAndApplyStockDeductionAsync(Product product, int quantityToDeduct)
        {
            if (!await _blockchainService.IsBlockchainAvailableAsync())
            {
                throw new InvalidOperationException("Blockchain ağı şu anda erişilemez durumda.");
            }

            var deductionPlan = BuildStockDeductionPlan(product, quantityToDeduct);

            var deductionActions = deductionPlan
                .GroupBy(plan => NormalizeLotNumber(plan.ChildProduct.LotNumber))
                .Select(group => new BlockchainActionRequest
                {
                    LotNumber = group.Key,
                    ActionType = 1, // Deducted
                    Quantity = group.Sum(plan => plan.Quantity),
                    FromLocation = "Merkez Depo",
                    ToLocation = "Laboratuvar"
                })
                .ToList();

            var txHash = await _blockchainService.LogActionsAsync(deductionActions);

            foreach (var plan in deductionPlan)
            {
                plan.ChildProduct.Quantity -= plan.Quantity;
                if (plan.ChildProduct.Quantity < 0)
                {
                    plan.ChildProduct.Quantity = 0;
                }

                _context.Update(plan.ChildProduct);
            }

            await _context.SaveChangesAsync();
            return txHash;
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

        private static List<StockDeductionPlan> BuildStockDeductionPlan(Product product, int quantityToDeduct)
        {
            if (quantityToDeduct <= 0)
            {
                throw new InvalidOperationException("Düşülecek miktar sıfırdan büyük olmalıdır.");
            }

            var availableLots = product.ChildProducts?
                .Where(childProduct => childProduct.Quantity > 0)
                .OrderBy(childProduct => childProduct.ExpirationDate)
                .ToList() ?? new List<ChildProduct>();

            var availableQuantity = availableLots.Sum(childProduct => childProduct.Quantity);
            if (availableLots.Count == 0 || availableQuantity < quantityToDeduct)
            {
                throw new InvalidOperationException($"Yetersiz stok. Mevcut stok: {availableQuantity}, istenen miktar: {quantityToDeduct}.");
            }

            var remainingQuantity = quantityToDeduct;
            var deductionPlan = new List<StockDeductionPlan>();

            foreach (var childProduct in availableLots)
            {
                if (remainingQuantity == 0)
                {
                    break;
                }

                var deductedQuantity = Math.Min(childProduct.Quantity, remainingQuantity);
                deductionPlan.Add(new StockDeductionPlan
                {
                    ChildProduct = childProduct,
                    Quantity = deductedQuantity
                });

                remainingQuantity -= deductedQuantity;
            }

            return deductionPlan;
        }



        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // GET: Product/BlockchainHistory?lotNumber=XXX
        public async Task<IActionResult> BlockchainHistory(string lotNumber)
        {
            if (string.IsNullOrEmpty(lotNumber))
            {
                return BadRequest("Lot numarası belirtilmedi.");
            }

            lotNumber = NormalizeLotNumber(lotNumber);
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
            var uninitializedLotStock = 0;
            var uninitializedLotNumbers = new List<string>();
            var blockchainLotStatuses = new List<BlockchainLotStatus>();
            string? blockchainCoverageError = null;

            if (isBcUp)
            {
                try
                {
                    var lotStocks = BuildPositiveLotSnapshots(product.ChildProducts);

                    foreach (var lotStock in lotStocks)
                    {
                        var records = await _blockchainService.GetHistoryAsync(lotStock.LotNumber);
                        var lotStatus = await _blockchainService.GetLotStatusAsync(lotStock.LotNumber);
                        blockchainLotStatuses.Add(lotStatus);

                        if (records != null)
                        {
                            allRecords.AddRange(records);
                            if (records.Count == 0 && lotStock.Quantity > 0)
                            {
                                uninitializedLotStock += lotStock.Quantity;
                                uninitializedLotNumbers.Add(lotStock.LotNumber);
                            }
                        }
                    }

                    allRecords = allRecords.OrderByDescending(r => r.Timestamp).ToList();
                }
                catch (Exception ex)
                {
                    blockchainCoverageError = ex.Message;
                    allRecords.Clear();
                    uninitializedLotStock = 0;
                    uninitializedLotNumbers.Clear();
                    blockchainLotStatuses.Clear();
                }
            }

            ViewBag.ProductName = product.ProductName;
            var databaseTotalStock = product.ChildProducts.Sum(c => c.Quantity);
            ViewBag.DatabaseTotalStock = databaseTotalStock;
            ViewBag.BlockchainComparableDatabaseStock = databaseTotalStock - uninitializedLotStock;
            ViewBag.UninitializedLotStock = uninitializedLotStock;
            ViewBag.UninitializedLotCount = uninitializedLotNumbers.Count;
            ViewBag.UninitializedLotNumbers = uninitializedLotNumbers;
            ViewBag.BlockchainLotStatuses = blockchainLotStatuses;
            ViewBag.BlockchainCoverageError = blockchainCoverageError;
            return View(allRecords);
        }

        private static List<ProductLotSnapshot> BuildPositiveLotSnapshots(IEnumerable<ChildProduct> productLots)
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
                .Select(group => new ProductLotSnapshot
                {
                    LotNumber = group.Key,
                    Quantity = group.Sum(childProduct => childProduct.Quantity)
                })
                .Where(snapshot => snapshot.Quantity > 0)
                .ToList();
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

        private sealed class ProductLotSnapshot
        {
            public string LotNumber { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
        // ─── Tedarik Zinciri Simülasyonu ───

        // GET: Product/SupplyChainSimulation
        [Authorize(Roles = "Admin,SuperUser")]
        public async Task<IActionResult> SupplyChainSimulation()
        {
            await EnsureSupplyChainReferenceDataAsync();

            var products = await _context.Products
                .Include(p => p.ChildProducts)
                .Include(p => p.Category)
                .ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
            ViewBag.Carriers = await _context.Carriers.Where(c => c.IsActive).ToListAsync();
            ViewBag.WarehouseLocations = await _context.WarehouseLocations.Where(w => w.IsActive).ToListAsync();
            ViewBag.LaboratoryLocations = await _context.LaboratoryLocations.Where(l => l.IsActive).ToListAsync();
            ViewBag.BlockchainAvailable = await _blockchainService.IsBlockchainAvailableAsync();
            return View(products);
        }

        // POST: Product/SimulateStep (AJAX endpoint)
        [Authorize(Roles = "Admin,SuperUser")]
        [HttpPost]
        public async Task<IActionResult> SimulateStep([FromBody] SimulationStepRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.LotNumber))
            {
                return Json(new { success = false, message = "Geçersiz istek." });
            }

            if (request.Quantity <= 0)
            {
                return Json(new { success = false, message = "Miktar sıfırdan büyük olmalıdır." });
            }

            try
            {
                if (!await _blockchainService.IsBlockchainAvailableAsync())
                {
                    return Json(new { success = false, message = "Blockchain ağı erişilemez durumda." });
                }

                request.LotNumber = NormalizeLotNumber(request.LotNumber);
                List<ChildProduct> productRowsForCoverage;
                if (request.ProductId > 0)
                {
                    productRowsForCoverage = await _context.ChildProducts
                        .Where(cp => cp.ProductId == request.ProductId)
                        .ToListAsync();
                }
                else
                {
                    var allRows = await _context.ChildProducts.ToListAsync();
                    productRowsForCoverage = allRows
                        .Where(cp => NormalizeLotNumber(cp.LotNumber) == request.LotNumber)
                        .ToList();
                }

                var lotRows = productRowsForCoverage
                    .Where(cp => NormalizeLotNumber(cp.LotNumber) == request.LotNumber)
                    .OrderBy(cp => cp.ExpirationDate)
                    .ToList();
                var currentLotQuantity = lotRows.Sum(cp => cp.Quantity);
                var childProductForDbUpdate = lotRows.FirstOrDefault();

                if (request.ActionType == 4 || request.ActionType == 6)
                {
                    if (childProductForDbUpdate == null)
                    {
                        return Json(new { success = false, message = "Lot veritabanında bulunamadı." });
                    }

                    if (request.ActionType == 6 && request.ToLocation == "Tüketildi" && currentLotQuantity < request.Quantity)
                    {
                        return Json(new { success = false, message = "Tüketilecek miktar veritabanındaki stoktan fazla olamaz." });
                    }
                }

                var txHash = await _blockchainService.LogActionAsync(
                    request.LotNumber,
                    request.ActionType,
                    request.Quantity,
                    request.FromLocation,
                    request.ToLocation
                );

                await SaveSupplyChainProcessRecordAsync(
                    request,
                    txHash,
                    childProductForDbUpdate,
                    request.ProductId > 0 ? request.ProductId : childProductForDbUpdate?.ProductId ?? 0);

                // Veritabanı stok güncellemesi
                if (request.ActionType == 4) // Received (Depoda Teslim Alındı)
                {
                    if (childProductForDbUpdate != null)
                    {
                        childProductForDbUpdate.Quantity += request.Quantity;
                        _context.Update(childProductForDbUpdate);
                        await _context.SaveChangesAsync();
                    }
                }
                else if (request.ActionType == 6) // Consumed (Tüketildi / Tamamlandı)
                {
                    if (request.ToLocation == "Tüketildi")
                    {
                        var remainingQuantity = request.Quantity;
                        foreach (var lotRow in lotRows)
                        {
                            if (remainingQuantity <= 0)
                            {
                                break;
                            }

                            var deductedQuantity = Math.Min(lotRow.Quantity, remainingQuantity);
                            lotRow.Quantity -= deductedQuantity;
                            remainingQuantity -= deductedQuantity;
                            _context.Update(lotRow);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                var lotStatus = await _blockchainService.GetLotStatusAsync(request.LotNumber);
                return Json(new
                {
                    success = true,
                    txHash = txHash,
                    lotNumber = lotStatus.LotNumber,
                    lotState = lotStatus.State,
                    onChainQuantity = lotStatus.OnChainQuantity,
                    pendingQuantity = lotStatus.PendingQuantity
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin,SuperUser")]
        [HttpGet]
        public async Task<IActionResult> GetLotStatus(string lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                return Json(new { success = false, message = "Lot numarası belirtilmedi." });
            }

            try
            {
                if (!await _blockchainService.IsBlockchainAvailableAsync())
                {
                    return Json(new { success = false, message = "Blockchain ağı erişilemez durumda." });
                }

                lotNumber = NormalizeLotNumber(lotNumber);
                var lotStatus = await _blockchainService.GetLotStatusAsync(lotNumber);
                return Json(new
                {
                    success = true,
                    lotNumber = lotStatus.LotNumber,
                    lotState = lotStatus.State,
                    onChainQuantity = lotStatus.OnChainQuantity,
                    pendingQuantity = lotStatus.PendingQuantity,
                    exists = lotStatus.Exists
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task EnsureSupplyChainReferenceDataAsync()
        {
            if (!await _context.Suppliers.AnyAsync())
            {
                _context.Suppliers.AddRange(
                    new Supplier { Name = "Tedarikçi 1", ContactName = "Üretim Birimi" },
                    new Supplier { Name = "Tedarikçi 2", ContactName = "Kalite Birimi" },
                    new Supplier { Name = "Tedarikçi 3", ContactName = "Lojistik Birimi" }
                );
            }

            if (!await _context.Carriers.AnyAsync())
            {
                _context.Carriers.Add(new Carrier { Name = "Standart Lojistik", ContactName = "Sevkiyat Ekibi" });
            }

            if (!await _context.WarehouseLocations.AnyAsync())
            {
                _context.WarehouseLocations.Add(new WarehouseLocation
                {
                    Name = "Merkez Numune Deposu",
                    Address = "Ana kabul ve stok noktası"
                });
            }

            if (!await _context.LaboratoryLocations.AnyAsync())
            {
                _context.LaboratoryLocations.Add(new LaboratoryLocation
                {
                    Name = "Klinik Biyokimya Lab.",
                    Department = "Biyokimya"
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SaveSupplyChainProcessRecordAsync(
            SimulationStepRequest request,
            string txHash,
            ChildProduct? childProduct,
            int productId)
        {
            if (productId <= 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var status = MapActionToProcessStatus(request.ActionType, request.ToLocation);

            if (request.ActionType == 2 || request.ActionType == 3 || request.ActionType == 4)
            {
                var shipment = await _context.SupplyChainShipments
                    .Where(s => s.ProductId == productId && s.LotNumber == request.LotNumber)
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefaultAsync();

                if (shipment == null || request.ActionType == 2)
                {
                    shipment = new SupplyChainShipment
                    {
                        ProductId = productId,
                        ChildProductId = childProduct?.Id,
                        SupplierId = request.SupplierId > 0 ? request.SupplierId : null,
                        CarrierId = request.CarrierId > 0 ? request.CarrierId : null,
                        WarehouseLocationId = request.WarehouseLocationId > 0 ? request.WarehouseLocationId : null,
                        LotNumber = request.LotNumber,
                        Quantity = request.Quantity,
                        CreatedAt = now
                    };
                    _context.SupplyChainShipments.Add(shipment);
                }

                shipment.Status = status;
                shipment.BlockchainTransactionHash = txHash;
                if (request.ActionType == 3)
                {
                    shipment.ShippedAt = now;
                }
                else if (request.ActionType == 4)
                {
                    shipment.ReceivedAt = now;
                }
            }

            if (request.ActionType == 4 || request.ActionType == 6)
            {
                _context.SupplyChainReceipts.Add(new SupplyChainReceipt
                {
                    ProductId = productId,
                    ChildProductId = childProduct?.Id,
                    LotNumber = request.LotNumber,
                    Quantity = request.Quantity,
                    FromLocation = request.FromLocation,
                    ToLocation = request.ToLocation,
                    Status = status,
                    BlockchainTransactionHash = txHash,
                    CreatedAt = now,
                    CompletedAt = now
                });
            }

            if (request.ActionType == 5 || request.ActionType == 6)
            {
                var transfer = await _context.SupplyChainTransfers
                    .Where(t => t.ProductId == productId && t.LotNumber == request.LotNumber)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                if (transfer == null || request.ActionType == 5)
                {
                    transfer = new SupplyChainTransfer
                    {
                        ProductId = productId,
                        ChildProductId = childProduct?.Id,
                        FromWarehouseLocationId = request.WarehouseLocationId > 0 ? request.WarehouseLocationId : null,
                        ToLaboratoryLocationId = request.LaboratoryLocationId > 0 ? request.LaboratoryLocationId : null,
                        LotNumber = request.LotNumber,
                        Quantity = request.Quantity,
                        CreatedAt = now
                    };
                    _context.SupplyChainTransfers.Add(transfer);
                }

                transfer.Status = status;
                transfer.BlockchainTransactionHash = txHash;
                if (request.ActionType == 5)
                {
                    transfer.TransferredAt = now;
                }
                else if (request.ActionType == 6)
                {
                    transfer.ConsumedAt = now;
                }
            }

            await _context.SaveChangesAsync();
        }

        private static SupplyChainProcessStatus MapActionToProcessStatus(int actionType, string toLocation)
        {
            return actionType switch
            {
                2 => SupplyChainProcessStatus.Produced,
                3 => SupplyChainProcessStatus.Shipped,
                4 => SupplyChainProcessStatus.Received,
                5 => SupplyChainProcessStatus.Transferred,
                6 when toLocation == "Tüketildi" => SupplyChainProcessStatus.Consumed,
                6 => SupplyChainProcessStatus.HeldInStock,
                _ => SupplyChainProcessStatus.Created
            };
        }

        private sealed class StockDeductionPlan
        {
            public ChildProduct ChildProduct { get; set; } = null!;
            public int Quantity { get; set; }
        }
    }

    // Simülasyon adımı istek modeli
    public class SimulationStepRequest
    {
        public int ProductId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public int ActionType { get; set; }
        public int Quantity { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public int CarrierId { get; set; }
        public int WarehouseLocationId { get; set; }
        public int LaboratoryLocationId { get; set; }
    }
}
