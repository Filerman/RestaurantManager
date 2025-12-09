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

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(string login, string password)
        {
            var user = _context.Users.AsNoTracking()
                .FirstOrDefault(u => (u.Username == login || u.Email == login) && u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "Nieprawidłowe dane logowania!";
                return View();
            }

            // Ustawienie sesji
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("ProfilePicturePath", user.ProfilePicturePath ?? DefaultAvatar);

            TempData["Success"] = "Zalogowano pomyślnie!";
            return RedirectToAction("Index", "Home");
        }

        // --- REGISTER ---
        [HttpGet]
        public IActionResult Register() => View();

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

        // --- LOGOUT ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Wylogowano pomyślnie.";
            return RedirectToAction("Login");
        }

        // --- PROFILE ---
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.AsNoTracking()
                .Include(u => u.Employee).ThenInclude(e => e.PositionTags)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath) ? DefaultAvatar : user.ProfilePicturePath
            };

            if (user.Employee != null)
            {
                vm.FullName = user.Employee.FullName;
                vm.Phone = user.Employee.Phone;
                vm.HireDate = user.Employee.HireDate;
                vm.PositionTags = user.Employee.PositionTags.ToList();

                // Obliczanie stażu pracy
                var today = DateTime.Now;
                var hired = user.Employee.HireDate;
                if (hired != DateTime.MinValue)
                {
                    int months = ((today.Year - hired.Year) * 12) + today.Month - hired.Month;
                    if (today.Day < hired.Day) months--;
                    int years = months / 12;
                    int remainingMonths = months % 12;
                    vm.SeniorityString = years > 0 ? $"{years} lat(a), {remainingMonths} mies." : $"{remainingMonths} mies.";
                }
            }
            return View(vm);
        }

        // --- EDIT PROFILE ---
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
                ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath) ? DefaultAvatar : user.ProfilePicturePath
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            // Walidacja Email
            if (vm.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == vm.Email && u.Id != user.Id))
                    ModelState.AddModelError("Email", "Ten email jest już zajęty.");
                else user.Email = vm.Email;
            }

            // Walidacja Hasła
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                if (vm.OldPassword != user.Password) ModelState.AddModelError("OldPassword", "Obecne hasło jest nieprawidłowe.");
                else if (vm.NewPassword != vm.ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "Hasła nie są identyczne.");
                else user.Password = vm.NewPassword;
            }

            if (!ModelState.IsValid)
            {
                vm.ProfilePicturePath = user.ProfilePicturePath ?? DefaultAvatar;
                return View(vm);
            }

            // Zapis Zdjęcia
            if (vm.ProfileImage != null && vm.ProfileImage.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _profileFolder);
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = $"u{user.Id}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(vm.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ProfileImage.CopyToAsync(stream);
                }

                // Usuń stare zdjęcie
                if (!string.IsNullOrEmpty(user.ProfilePicturePath) && user.ProfilePicturePath != DefaultAvatar)
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) try { System.IO.File.Delete(oldPath); } catch { }
                }

                user.ProfilePicturePath = "/" + Path.Combine(_profileFolder, fileName).Replace("\\", "/");
                HttpContext.Session.SetString("ProfilePicturePath", user.ProfilePicturePath);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profil zaktualizowany.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }
    }
}