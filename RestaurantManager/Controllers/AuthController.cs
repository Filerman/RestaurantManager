using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RestaurantManager.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private const string DefaultAvatar = "/images/profiles/default.png";
        private readonly string _profileFolder = "images/profiles";

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login() => View();

        // POST: /Auth/Login
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(string login, string password)
        {
            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u =>
                    (u.Username == login || u.Email == login) &&
                    u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "Nieprawidłowe dane logowania!";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("ProfilePicturePath", user.ProfilePicturePath ?? DefaultAvatar);

            TempData["Success"] = "Zalogowano pomyślnie!";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Auth/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
            {
                TempData["Error"] = "Nazwa użytkownika lub email jest już zajęty!";
                return View();
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = password,
                Role = "Guest",
                ProfilePicturePath = DefaultAvatar
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rejestracja udana! Możesz się teraz zalogować.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Wylogowano pomyślnie.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                    .ThenInclude(e => e.PositionTags)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath
            };

            if (user.Employee != null)
            {
                vm.FullName = user.Employee.FullName;
                vm.Phone = user.Employee.Phone;
                vm.HireDate = user.Employee.HireDate;
                vm.PositionTags = user.Employee.PositionTags.ToList();

                var today = DateTime.Now;
                var hired = user.Employee.HireDate;

                if (hired != DateTime.MinValue)
                {
                    int months = ((today.Year - hired.Year) * 12) + today.Month - hired.Month;
                    if (today.Day < hired.Day) months--;

                    int years = months / 12;
                    int remainingMonths = months % 12;

                    if (years > 0)
                        vm.SeniorityString = $"{years} lat(a), {remainingMonths} mies.";
                    else
                        vm.SeniorityString = $"{remainingMonths} mies.";

                    if (years == 0 && remainingMonths == 0)
                    {
                        var days = (today - hired).Days;
                        vm.SeniorityString = $"{days} dni";
                    }
                }
            }

            if (TempData["Success"] != null)
                ViewBag.Success = TempData["Success"];

            return View(vm);
        }

        // GET: /Auth/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath
            };

            return View(vm);
        }

        // POST: /Auth/EditProfile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            // 1) Email
            if (vm.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == vm.Email && u.Id != user.Id))
                    ModelState.AddModelError("Email", "Ten email jest już zajęty.");
                else
                    user.Email = vm.Email;
            }

            // 2) Hasło
            if (!string.IsNullOrWhiteSpace(vm.OldPassword)
                || !string.IsNullOrWhiteSpace(vm.NewPassword)
                || !string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                if (vm.OldPassword != user.Password)
                    ModelState.AddModelError("OldPassword", "Obecne hasło jest nieprawidłowe.");
                if (vm.NewPassword != vm.ConfirmPassword)
                    ModelState.AddModelError("ConfirmPassword", "Hasła nie są identyczne.");
                if (ModelState.IsValid && !string.IsNullOrEmpty(vm.NewPassword))
                    user.Password = vm.NewPassword;
            }

            if (!ModelState.IsValid)
            {
                vm.ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath;
                return View(vm);
            }

            // 3) ZDJĘCIE - POPRAWIONA LOGIKA BEZPOŚREDNIA
            if (vm.ProfileImage != null && vm.ProfileImage.Length > 0)
            {
                // Tworzymy ścieżkę do folderu wwwroot/images/profiles
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _profileFolder);

                // Upewnij się, że katalog istnieje
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                // Generujemy unikalną nazwę pliku
                var fileName = $"u{user.Id}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(vm.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploads, fileName);

                // Zapisujemy plik na dysku
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ProfileImage.CopyToAsync(stream);
                }

                // Usuwamy stare zdjęcie, jeśli to nie jest domyślny avatar
                if (!string.IsNullOrEmpty(user.ProfilePicturePath) && user.ProfilePicturePath != DefaultAvatar)
                {
                    // Usuwamy pierwszy slash z ścieżki (np. "/images/..." -> "images/...")
                    var relativePath = user.ProfilePicturePath.TrimStart('/');
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                    if (System.IO.File.Exists(oldPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPath);
                        }
                        catch { /* Ignorujemy błędy usuwania starego pliku */ }
                    }
                }

                // Aktualizujemy ścieżkę w bazie danych
                user.ProfilePicturePath = "/" + Path.Combine(_profileFolder, fileName).Replace("\\", "/");

                // Aktualizujemy sesję, aby nagłówek strony od razu pokazał nowe zdjęcie
                HttpContext.Session.SetString("ProfilePicturePath", user.ProfilePicturePath);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profil zaktualizowany.";
            return RedirectToAction("Profile");
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }
    }
}