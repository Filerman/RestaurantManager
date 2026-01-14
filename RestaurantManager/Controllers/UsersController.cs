using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RestaurantManager.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        // Definicja dostępnych ról
        private readonly List<string> _roles = new List<string> { "Guest", "Employee", "Manager", "Admin" };

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> Directory(string searchString, string filter = "all")
        {
            var usersQuery = _context.Users
                .Include(u => u.Employee) // Dołączamy dane pracownika (telefon, imię)
                .AsNoTracking();

            // 1. Filtrowanie po typie (Wszyscy / Pracownicy / Klienci)
            switch (filter)
            {
                case "employees":
                    // Pracownicy to: Admin, Manager, Employee
                    usersQuery = usersQuery.Where(u => u.Role == "Admin" || u.Role == "Manager" || u.Role == "Employee");
                    break;
                case "clients":
                    // Klienci to: Guest
                    usersQuery = usersQuery.Where(u => u.Role == "Guest");
                    break;
                case "all":
                default:
                    // Brak dodatkowego filtra
                    break;
            }

            // 2. Wyszukiwanie (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Username.ToLower().Contains(searchString) ||
                    u.Email.ToLower().Contains(searchString) ||
                    (u.Employee != null && u.Employee.FullName.ToLower().Contains(searchString)));
            }

            var users = await usersQuery
                .OrderByDescending(u => u.Role) // Najpierw ważniejsze role
                .ThenBy(u => u.Username)
                .ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["SelectedFilter"] = filter; // Przekazujemy wybrany filtr do widoku

            return View(users);
        }

        // GET: Users
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Employee) // Warto dołączyć employee też tutaj
                .ThenInclude(e => e.PositionTags)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Users/Edit/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var editorRole = HttpContext.Session.GetString("UserRole");

            bool canEdit = true;
            if (user.Role == "Admin" && editorRole == "Manager")
            {
                canEdit = false;
                ViewBag.CannotEditMessage = "Manager nie może edytować konta Administratora.";
            }
            ViewBag.CanEdit = canEdit;

            ViewBag.Roles = new SelectList(_roles, user.Role);

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,Role")] User userForm)
        {
            if (id != userForm.Id) return NotFound();

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            var editorRole = HttpContext.Session.GetString("UserRole");
            if (userToUpdate.Role == "Admin" && editorRole == "Manager")
            {
                TempData["ErrorMessage"] = "Manager nie może modyfikować konta Administratora.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Password");
            ModelState.Remove("Employee");
            ModelState.Remove("Reservations");
            ModelState.Remove("Availabilities");
            ModelState.Remove("Shifts"); // Dodano na wszelki wypadek

            if (await _context.Users.AnyAsync(u => u.Id != id && u.Username == userForm.Username))
            {
                ModelState.AddModelError("Username", "Ta nazwa użytkownika jest już zajęta.");
            }
            if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == userForm.Email))
            {
                ModelState.AddModelError("Email", "Ten adres email jest już zajęty.");
            }
            if (!_roles.Contains(userForm.Role))
            {
                ModelState.AddModelError("Role", "Wybrano nieprawidłową rolę.");
            }
            if (userForm.Role == "Admin" && editorRole == "Manager")
            {
                ModelState.AddModelError("Role", "Manager nie może nadać roli Administratora.");
            }

            if (ModelState.IsValid)
            {
                userToUpdate.Username = userForm.Username;
                userToUpdate.Email = userForm.Email;
                userToUpdate.Role = userForm.Role;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == userToUpdate.Id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania danych.");
                    ViewBag.Roles = new SelectList(_roles, userForm.Role);
                    ViewBag.CanEdit = true;
                    return View(userForm);
                }
                TempData["SuccessMessage"] = "Dane użytkownika zostały zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_roles, userForm.Role);
            ViewBag.CanEdit = !(userToUpdate.Role == "Admin" && editorRole == "Manager");
            return View(userForm);
        }

        // GET: Users/Delete/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            var editorRole = HttpContext.Session.GetString("UserRole");
            if (user.Role == "Admin" && editorRole == "Manager")
            {
                TempData["ErrorMessage"] = "Manager nie może usunąć konta Administratora.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var editorRole = HttpContext.Session.GetString("UserRole");
                if (user.Role == "Admin" && editorRole == "Manager")
                {
                    TempData["ErrorMessage"] = "Manager nie może usunąć konta Administratora.";
                    return RedirectToAction(nameof(Index));
                }

                if ((user.Role == "Admin" || user.Role == "Manager") &&
                    await _context.Users.CountAsync(u => u.Role == "Admin" || u.Role == "Manager") <= 1)
                {
                    TempData["ErrorMessage"] = "Nie można usunąć ostatniego Administratora/Managera.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Użytkownik został usunięty.";
            }
            return RedirectToAction(nameof(Index));
        }


        // GET: Users/AdminResetPassword/5
        [Filters.RoleAuthorize("Admin")]
        public async Task<IActionResult> AdminResetPassword(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/AdminResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Filters.RoleAuthorize("Admin")]
        public async Task<IActionResult> AdminResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
            {
                ModelState.AddModelError("", "Hasło musi mieć co najmniej 4 znaki.");
                return View(user);
            }

            // Nadpisujemy hasło (bez sprawdzania starego!)
            user.Password = newPassword;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Hasło dla użytkownika {user.Username} zostało zresetowane.";
            return RedirectToAction(nameof(Index));
        }
    }
}