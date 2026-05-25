using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;

namespace NumuneStok.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;


    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;

    }

    [Authorize]
    public IActionResult Index()
    {
        // Kullanıcı sayısı
        var userCount = _context.Users.Count();

        // Ürün çeşiti (farklı ürün sayısı)
        var productVariety = _context.Products.Count();

        // Toplam ürün sayısı (tüm ürünlerin toplamı)
        var totalProductCount = _context.ChildProducts.Sum(p => p.Quantity);

        // Saat (server saatini gösterebiliriz)
        var currentHour = DateTime.Now.ToString("HH:mm");

        var expiredProduct = _context.ChildProducts
                .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
                .AsEnumerable() // SQL sorgusundan sonra bellek işlemleri için AsEnumerable() kullanıyoruz
                .Where(cp => (cp.ExpirationDate - DateTime.Now).TotalDays <= 30) // Bellek üzerinde filtreleme yapıyoruz
                .Count(); // Listeye çeviriyoruz

        var lowStock = _context.ChildProducts
                .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
                .Where(cp => cp.Quantity <= 5) // Adet’i 5'in altında olan ürünleri filtreliyoruz
                .Count(); // Listeye çeviriyoruz

        var expired = _context.ChildProducts
        .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
        .AsEnumerable() // SQL sorgusundan sonra bellek işlemleri için AsEnumerable() kullanıyoruz
        .Where(cp => (cp.ExpirationDate - DateTime.Now).TotalDays <= 30) // Bellek üzerinde filtreleme yapıyoruz
        .ToList(); // Listeye çeviriyoruz

        // Miadı yaklaşan ürün isimlerini al
        var expiredProductNames = expired.Select(cp => cp.Product.ProductName).ToList();


        // ViewBag kullanarak bu verileri view'a aktaralım
        ViewBag.UserCount = userCount;
        ViewBag.ProductVariety = productVariety;
        ViewBag.TotalProductCount = totalProductCount;
        ViewBag.CurrentHour = currentHour;
        ViewBag.Expired = expiredProduct;
        ViewBag.Low = lowStock;
        ViewBag.ExpiredProductNames = expiredProductNames; // Ürün isimlerini ViewBag'e ekle

        return View();
    }

    [Authorize]
    public IActionResult Visitor()
    {
        return View();
    }

    [Authorize]
    public IActionResult Super()
    {
        // Kullanıcı sayısı
        var userCount = _context.Users.Count();

        // Ürün çeşiti (farklı ürün sayısı)
        var productVariety = _context.Products.Count();

        // Toplam ürün sayısı (tüm ürünlerin toplamı)
        var totalProductCount = _context.ChildProducts.Sum(p => p.Quantity);

        // Saat (server saatini gösterebiliriz)
        var currentHour = DateTime.Now.ToString("HH:mm");

        var expiredProduct = _context.ChildProducts
                .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
                .AsEnumerable() // SQL sorgusundan sonra bellek işlemleri için AsEnumerable() kullanıyoruz
                .Where(cp => (cp.ExpirationDate - DateTime.Now).TotalDays <= 30) // Bellek üzerinde filtreleme yapıyoruz
                .Count(); // Listeye çeviriyoruz

        var lowStock = _context.ChildProducts
                .Include(cp => cp.Product) // Parent product bilgilerini dahil ediyoruz
                .Where(cp => cp.Quantity <= 5) // Adet’i 5'in altında olan ürünleri filtreliyoruz
                .Count(); // Listeye çeviriyoruz 

        // ViewBag kullanarak bu verileri view'a aktaralım
        ViewBag.UserCount = userCount;
        ViewBag.ProductVariety = productVariety;
        ViewBag.TotalProductCount = totalProductCount;
        ViewBag.CurrentHour = currentHour;
        ViewBag.Expired = expiredProduct;
        ViewBag.Low = lowStock;

        return View();
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

