using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
//using NumuneStok.Data;  Proje adını buraya uygun şekilde değiştirin
using NumuneStok.Models; // User modeli için uygun yolu değiştirin
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NumuneStok.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Giriş Yap GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Giriş Yap POST
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.UserName == username && u.Password == hashedPassword);

            if (user != null)
            {
                // Kullanıcı bilgilerini içeren kimlik (Claim) listesi oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role.RoleName),
                    new Claim("UserId", user.Id.ToString()) // Kullanıcı ID'sini saklamak için
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true // Tarayıcı kapansa bile girişin açık kalmasını sağlar (isteğe bağlı)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // Kullanıcı rolüne göre yönlendirme
                switch (user.Role.RoleName)
                {
                    case "Admin":
                        return RedirectToAction("Index", "Home");
                    case "SuperUser":
                        return RedirectToAction("Super", "Home");
                    case "Visitor":
                        return RedirectToAction("Visitor", "Home");
                    default:
                        ViewBag.Error = "Geçersiz rol.";
                        return View();
                }
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }


        // Kayıt Ol GET
        [Authorize(Roles = "SuperUser")]
        [HttpGet]
        public IActionResult Register()
        {
            var roles = _context.Roles.ToList();
            ViewBag.Roles = roles;
            return View();
        }

        // Kayıt Ol POST
        [Authorize(Roles = "SuperUser")]
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, int roleId)
        {
            var existingUser = await _context.Users.AnyAsync(u => u.UserName == username);
            if (existingUser)
            {
                ViewBag.Error = "Bu kullanıcı adı zaten alınmış.";
                return View();
            }

            var newUser = new User
            {
                UserName = username,
                Password = HashPassword(password),
                RoleId = roleId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("Super", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
