using System; // Ważne dla DateTime
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels; // Dodaj ten using dla PayrollViewModel
using RestaurantManager.Filters;
using System.Collections.Generic;

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

        // --- NOWA FUNKCJONALNOŚĆ: WYPŁATY ---

        [HttpGet]
        public async Task<IActionResult> Payroll(int? month, int? year)
        {
            // Domyślnie obecny miesiąc i rok
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            // 1. Pobierz wszystkie zmiany z danego miesiąca, które mają przypisanego pracownika i tag stanowiska
            var shifts = await _context.Shifts
                .Where(s => s.Date.Month == selectedMonth && s.Date.Year == selectedYear && s.UserId != null && s.PositionTagId != null)
                .Include(s => s.EmployeeUser).ThenInclude(u => u.Employee)
                .Include(s => s.ShiftPositionTag) // Ważne: pobieramy stawkę z tagu
                .ToListAsync();

            // 2. Grupuj zmiany po pracowniku i oblicz wypłatę
            var payrollList = shifts
                .GroupBy(s => s.UserId)
                .Select(g => {
                    var employee = g.First().EmployeeUser?.Employee;
                    var user = g.First().EmployeeUser;

                    double totalHours = g.Sum(s => (s.EndTime - s.StartTime).TotalHours);

                    // Obliczenie pensji: Suma (Godziny zmiany * Stawka tagu tej zmiany)
                    decimal totalSalary = g.Sum(s =>
                        (decimal)(s.EndTime - s.StartTime).TotalHours * (s.ShiftPositionTag?.HourlyRate ?? 0)
                    );

                    return new PayrollViewModel
                    {
                        EmployeeName = employee?.FullName ?? "Nieznany",
                        EmployeeEmail = user?.Email ?? "-",
                        ShiftsCount = g.Count(),
                        TotalHours = Math.Round(totalHours, 2),
                        TotalSalary = totalSalary
                    };
                })
                .OrderBy(p => p.EmployeeName)
                .ToList();

            var vm = new PayrollIndexViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                Payrolls = payrollList
            };

            return View(vm);
        }

        // -------------------------------------

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

            ViewData["AllTags"] = await _context.PositionTags.ToListAsync();
            ViewData["SelectedTagIds"] = employee.PositionTags.Select(t => t.Id).ToHashSet();
            ViewData["EmployeeEmail"] = user.Email;

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            // USUNIĘTO HourlyRate z listy pól do aktualizacji
            if (await TryUpdateModelAsync<Employee>(
                employeeToUpdate,
                "",
                e => e.FullName, e => e.Phone))
            {
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
                    ModelState.AddModelError("", "Nie można zapisać zmian.");
                    ViewData["AllTags"] = await _context.PositionTags.ToListAsync();
                    ViewData["SelectedTagIds"] = new HashSet<int>(selectedTagIds);
                    var user = await _context.Users.FindAsync(id);
                    ViewData["EmployeeEmail"] = user.Email;
                    return View(employeeToUpdate);
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["AllTags"] = await _context.PositionTags.ToListAsync();
            ViewData["SelectedTagIds"] = new HashSet<int>(selectedTagIds);
            var userFailed = await _context.Users.FindAsync(id);
            ViewData["EmployeeEmail"] = userFailed.Email;
            return View(employeeToUpdate);
        }

        private void UpdateEmployeeTags(int[] selectedTagIds, Employee employeeToUpdate)
        {
            if (selectedTagIds == null || !selectedTagIds.Any())
            {
                employeeToUpdate.PositionTags = new HashSet<PositionTag>();
                return;
            }

            var selectedTagsHS = new HashSet<int>(selectedTagIds);
            var employeeTags = new HashSet<int>(employeeToUpdate.PositionTags.Select(t => t.Id));

            foreach (var tag in _context.PositionTags)
            {
                if (selectedTagsHS.Contains(tag.Id))
                {
                    if (!employeeTags.Contains(tag.Id))
                    {
                        employeeToUpdate.PositionTags.Add(tag);
                    }
                }
                else
                {
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
            var employee = await _context.Employees.FindAsync(id); // Może być null

            if (employee != null) _context.Employees.Remove(employee);
            if (user != null) _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}