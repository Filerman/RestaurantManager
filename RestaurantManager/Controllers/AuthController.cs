using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RestaurantManager.Data;
using RestaurantManager.Models;
using System.IO;
using System.Linq;

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
                .FirstOrDefault(u =>
                    (u.Username == login || u.Email == login) &&
                    u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "Invalid credentials!";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["Success"] = "Logged in successfully!";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Auth/Register
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(string username, string email, string password)
        {
            if (_context.Users.Any(u => u.Username == username || u.Email == email))
            {
                TempData["Error"] = "Username or email is already taken!";
                return View();
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = password,
                Role = "Guest",
                // ustawiamy od razu defaultowe zdjęcie
                ProfilePicturePath = DefaultAvatar
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["Success"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = _context.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                ExistingPicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath
            };

            if (TempData["Success"] != null)
                ViewBag.Success = TempData["Success"];

            return View(vm);
        }

        // GET: /Auth/EditProfile
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = _context.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                ExistingPicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath
            };

            return View(vm);
        }

        // POST: /Auth/EditProfile
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditProfile(ProfileViewModel vm)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = _context.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login");

            // 1) Email
            if (vm.Email != user.Email)
            {
                if (_context.Users.Any(u => u.Email == vm.Email && u.Id != user.Id))
                    ModelState.AddModelError("Email", "This email is already taken.");
                else
                    user.Email = vm.Email;
            }

            // 2) Password change
            if (!string.IsNullOrWhiteSpace(vm.OldPassword)
                || !string.IsNullOrWhiteSpace(vm.NewPassword)
                || !string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                if (vm.OldPassword != user.Password)
                    ModelState.AddModelError("OldPassword", "Current password is incorrect.");
                if (vm.NewPassword != vm.ConfirmPassword)
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                if (ModelState.IsValid && !string.IsNullOrEmpty(vm.NewPassword))
                    user.Password = vm.NewPassword;
            }

            // Jeśli są błędy, wróć do formularza
            if (!ModelState.IsValid)
            {
                vm.ExistingPicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                    ? DefaultAvatar
                    : user.ProfilePicturePath;
                return View(vm);
            }

            // 3) Profile picture upload
            if (vm.ProfileImage is { Length: > 0 })
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _profileFolder);
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                var fileName = $"u{user.Id}_{Path.GetRandomFileName()}{Path.GetExtension(vm.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploads, fileName);
                using var stream = System.IO.File.Create(filePath);
                vm.ProfileImage.CopyTo(stream);

                // Usuń stare, tylko jeśli nie jest to default.png
                if (!string.IsNullOrEmpty(user.ProfilePicturePath)
                    && user.ProfilePicturePath != DefaultAvatar)
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                user.ProfilePicturePath = "/" + Path.Combine(_profileFolder, fileName)
                                              .Replace("\\", "/");
            }

            // Zapis i redirect
            _context.SaveChanges();
            TempData["Success"] = "Profile updated.";
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
