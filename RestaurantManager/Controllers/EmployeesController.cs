using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Collections.Generic; // Dodaj ten using

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var employeesAsUsers = await _context.Users
                .Where(u => u.Role == "Employee")
                .Include(u => u.Employee)
                    .ThenInclude(e => e.PositionTags)
                .ToListAsync();

            return View(employeesAsUsers);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.PositionTags)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null || user.Role != "Employee") return NotFound();

            return View(user);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Employee") return NotFound();

            var employee = await _context.Employees
                .Include(e => e.PositionTags)
                .FirstOrDefaultAsync(e => e.UserId == id);

            if (employee == null)
            {
                employee = new Employee
                {
                    UserId = user.Id,
                    FullName = user.Username
                };
            }

            // *** ZMIANA NA POTRZEBY CHECKBOXÓW ***
            // Przekazujemy pełną listę wszystkich tagów
            ViewData["AllTags"] = await _context.PositionTags.ToListAsync();

            // Przekazujemy zbiór ID tagów, które pracownik już ma
            ViewData["SelectedTagIds"] = employee.PositionTags.Select(t => t.Id).ToHashSet();

            ViewData["EmployeeEmail"] = user.Email;

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Akcja akceptuje `id` (z trasy) i `selectedTagIds` (z checkboxów)
        public async Task<IActionResult> Edit(int id, int[] selectedTagIds)
        {
            var employeeToUpdate = await _context.Employees
                .Include(e => e.PositionTags)
                .FirstOrDefaultAsync(e => e.UserId == id);

            bool isNew = false;
            if (employeeToUpdate == null)
            {
                isNew = true;
                employeeToUpdate = new Employee { UserId = id };
            }

            // Mapujemy dane z formularza do modelu
            if (await TryUpdateModelAsync<Employee>(
                employeeToUpdate,
                "", // Pusty prefiks
                e => e.FullName, e => e.Phone, e => e.HourlyRate))
            {
                // Aktualizujemy tagi na podstawie zaznaczonych checkboxów
                UpdateEmployeeTags(selectedTagIds, employeeToUpdate);

                if (isNew)
                {
                    _context.Employees.Add(employeeToUpdate);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Nie można zapisać zmian. Sprawdź poprawność danych (np. format telefonu).");
                    // Jeśli zapis się nie powiedzie, musimy ponownie załadować dane dla widoku
                    ViewData["AllTags"] = await _context.PositionTags.ToListAsync();
                    ViewData["SelectedTagIds"] = new HashSet<int>(selectedTagIds); // Użyj tagów, które użytkownik próbował zapisać
                    var user = await _context.Users.FindAsync(id);
                    ViewData["EmployeeEmail"] = user.Email;
                    return View(employeeToUpdate);
                }
                return RedirectToAction(nameof(Index));
            }

            // Jeśli model jest niepoprawny (np. błąd walidacji)
            ViewData["AllTags"] = await _context.PositionTags.ToListAsync();
            ViewData["SelectedTagIds"] = new HashSet<int>(selectedTagIds);
            var userFailed = await _context.Users.FindAsync(id);
            ViewData["EmployeeEmail"] = userFailed.Email;
            return View(employeeToUpdate);
        }

        // Metoda pomocnicza - działa poprawnie z listą ID z checkboxów
        private void UpdateEmployeeTags(int[] selectedTagIds, Employee employeeToUpdate)
        {
            // Jeśli nie przyszły żadne ID, wyczyść tagi
            if (selectedTagIds == null || !selectedTagIds.Any())
            {
                employeeToUpdate.PositionTags = new HashSet<PositionTag>();
                return;
            }

            var selectedTagsHS = new HashSet<int>(selectedTagIds);
            var employeeTags = new HashSet<int>(employeeToUpdate.PositionTags.Select(t => t.Id));

            // Przejdź po wszystkich tagach w systemie
            foreach (var tag in _context.PositionTags)
            {
                if (selectedTagsHS.Contains(tag.Id))
                {
                    // Jeśli tag jest wybrany i pracownik go nie ma, dodaj
                    if (!employeeTags.Contains(tag.Id))
                    {
                        employeeToUpdate.PositionTags.Add(tag);
                    }
                }
                else
                {
                    // Jeśli tag nie jest wybrany, a pracownik go ma, usuń
                    if (employeeTags.Contains(tag.Id))
                    {
                        var tagToRemove = employeeToUpdate.PositionTags.FirstOrDefault(t => t.Id == tag.Id);
                        if (tagToRemove != null)
                        {
                            employeeToUpdate.PositionTags.Remove(tagToRemove);
                        }
                    }
                }
            }
        }


        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Employee.PositionTags)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null || user.Role != "Employee") return NotFound();

            return View(user);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            var employee = await _context.Employees.FindAsync(id);

            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }
            if (user != null)
            {
                // Usunięcie użytkownika automatycznie usunie pracownika
                // (dzięki onDelete: ReferentialAction.Cascade w migracji),
                // ale jawne usunięcie obu jest bezpieczniejsze.
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}