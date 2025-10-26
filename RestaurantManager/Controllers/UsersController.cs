using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Collections.Generic; // Dodano dla List<string>
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Potrzebne do odczytu roli z sesji

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")] // Tylko Admin i Manager
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        // Definicja dostępnych ról
        private readonly List<string> _roles = new List<string> { "Guest", "Employee", "Manager", "Admin" };

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            // Pobieramy wszystkich użytkowników i sortujemy
            var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }


        // GET: Users/Edit/5 (ZMIENIONY - przekazuje informację do widoku)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Pobierz rolę zalogowanego użytkownika
            var editorRole = HttpContext.Session.GetString("UserRole");

            // *** ZMIANA: Zamiast przekierowywać, przekaż informację do widoku ***
            bool canEdit = true;
            if (user.Role == "Admin" && editorRole == "Manager")
            {
                canEdit = false;
                ViewBag.CannotEditMessage = "Manager nie może edytować konta Administratora.";
            }
            ViewBag.CanEdit = canEdit; // Przekaż informację do widoku
            // *** KONIEC ZMIANY ***

            // Przygotowujemy listę ról dla dropdown
            ViewBag.Roles = new SelectList(_roles, user.Role);

            return View(user);
        }

        // POST: Users/Edit/5 (ZMIENIONY - dodano ModelState.Remove i poprawki)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,Role")] User userForm)
        {
            if (id != userForm.Id) return NotFound();

            // Pobieramy ORYGINALNEGO użytkownika z bazy, aby zachować hasło i inne pola
            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // Zabezpieczenie: Manager nie może edytować Admina
            var editorRole = HttpContext.Session.GetString("UserRole");
            if (userToUpdate.Role == "Admin" && editorRole == "Manager")
            {
                TempData["ErrorMessage"] = "Manager nie może modyfikować konta Administratora.";
                return RedirectToAction(nameof(Index));
            }

            // POPRAWKA ZAPISU: Usuwamy powiązane encje z walidacji
            ModelState.Remove("Password"); // Hasła nie edytujemy tutaj
            ModelState.Remove("Employee");
            ModelState.Remove("Reservations");
            ModelState.Remove("Availabilities");

            // Walidacja unikalności i roli
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
            // Jeśli manager próbuje nadać komuś rolę Admina - zabroń
            if (userForm.Role == "Admin" && editorRole == "Manager")
            {
                ModelState.AddModelError("Role", "Manager nie może nadać roli Administratora.");
            }

            if (ModelState.IsValid)
            {
                // Aktualizujemy tylko te pola, które chcemy zmienić
                userToUpdate.Username = userForm.Username;
                userToUpdate.Email = userForm.Email;
                userToUpdate.Role = userForm.Role;
                // Hasło i inne pola (np. ProfilePicturePath) pozostają bez zmian

                try
                {
                    // _context.Update(userToUpdate); // Niepotrzebne, EF Core śledzi zmiany
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == userToUpdate.Id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException ex) // Lepsze łapanie błędów zapisu
                {
                    // Można dodać logowanie błędu ex
                    ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania danych.");
                    // Jeśli błąd, musimy ponownie przygotować widok
                    ViewBag.Roles = new SelectList(_roles, userForm.Role);
                    ViewBag.CanEdit = true; // Bo doszło do błędu zapisu, a nie braku uprawnień
                    return View(userForm);
                }
                TempData["SuccessMessage"] = "Dane użytkownika zostały zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }

            // Jeśli model jest niepoprawny, ponownie załaduj role i zwróć widok
            ViewBag.Roles = new SelectList(_roles, userForm.Role);
            // Przekaż informację o możliwości edycji ponownie
            ViewBag.CanEdit = !(userToUpdate.Role == "Admin" && editorRole == "Manager");
            return View(userForm); // Zwracamy model z błędami, aby użytkownik widział co poprawić
        }


        // GET: Users/Delete/5 (ZMIENIONY - dodano zabezpieczenie)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            // ZABEZPIECZENIE (Widok): Manager nie może nawet wejść na stronę usuwania Admina
            var editorRole = HttpContext.Session.GetString("UserRole");
            if (user.Role == "Admin" && editorRole == "Manager")
            {
                TempData["ErrorMessage"] = "Manager nie może usunąć konta Administratora.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Users/Delete/5 (ZMIENIONY - dodano zabezpieczenie)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // ZABEZPIECZENIE: Manager nie może usunąć Admina
                var editorRole = HttpContext.Session.GetString("UserRole");
                if (user.Role == "Admin" && editorRole == "Manager")
                {
                    TempData["ErrorMessage"] = "Manager nie może usunąć konta Administratora.";
                    return RedirectToAction(nameof(Index));
                }

                // Sprawdź czy to nie jest ostatni Admin/Manager
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
    }
}