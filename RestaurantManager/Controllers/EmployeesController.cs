using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Filters;
using RestaurantManager.Models;
using System.Linq;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Manager", "Admin")]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _ctx;
        private const string DefaultAvatar = "/images/profiles/default.png";

        public EmployeesController(AppDbContext ctx) => _ctx = ctx;

        // GET: /Employees
        public IActionResult Index(string searchTerm)
        {
            var q = _ctx.Users
                        .Where(u => u.Role == "Employee");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                q = q.Where(u =>
                    u.Username.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm));
            }

            var list = q
                .OrderBy(u => u.Username)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.DefaultAvatar = DefaultAvatar;
            return View(list);
        }

        // GET: /Employees/Details/5
        public IActionResult Details(int id)
        {
            var user = _ctx.Users
                .Where(u => u.Id == id && u.Role == "Employee")
                .Select(u => new ProfileViewModel
                {
                    Username = u.Username,
                    Email = u.Email,
                    ExistingPicturePath = string.IsNullOrEmpty(u.ProfilePicturePath)
                        ? DefaultAvatar
                        : u.ProfilePicturePath
                })
                .FirstOrDefault();

            if (user == null)
                return NotFound();

            return View(user);
        }

        // GET: /Employees/Create
        [HttpGet]
        public IActionResult Create() => View();

        // POST: /Employees/Create
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(string username, string email, string password)
        {
            if (_ctx.Users.Any(u => u.Username == username || u.Email == email))
            {
                TempData["Error"] = "Username or email is already taken!";
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Password = password,
                Role = "Employee",
                ProfilePicturePath = DefaultAvatar
            };

            _ctx.Users.Add(user);
            _ctx.SaveChanges();
            TempData["Success"] = "Employee account created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employees/Delete/5
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var user = _ctx.Users.Find(id);
            if (user == null || user.Role != "Employee")
                return NotFound();
            return View(user);
        }

        // POST: /Employees/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _ctx.Users.Find(id);
            if (user != null && user.Role == "Employee")
            {
                _ctx.Users.Remove(user);
                _ctx.SaveChanges();
                TempData["Success"] = "Employee account deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
