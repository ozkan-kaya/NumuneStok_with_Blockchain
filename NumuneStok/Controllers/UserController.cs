using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;
using System.Linq;

namespace NumuneStok.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Kullanıcıları listele
        public IActionResult Index()
        {
            var users = _context.Users.Include(u => u.Role).ToList(); // Include burada kullanılacak
            return View(users);
        }

        // Kullanıcıyı sil
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
