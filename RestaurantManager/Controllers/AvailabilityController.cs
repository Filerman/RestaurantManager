using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using RestaurantManager.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class AvailabilityController : Controller
    {
        private readonly AppDbContext _context;

        public AvailabilityController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Availability
        // POPRAWKA: Dodano parametr targetUserId do obsługi podglądu
        public async Task<IActionResult> Index(int? targetUserId)
        {
            var loggedUserId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (!loggedUserId.HasValue) return RedirectToAction("Login", "Auth");

            int userIdToDisplay = loggedUserId.Value;
            bool isViewingOthers = false;
            string titleHeader = "Moja Dostępność";

            // LOGIKA PODGLĄDU:
            // Jeśli podano targetUserId I jest on inny niż mój ID I mam uprawnienia Managera/Admina
            if (targetUserId.HasValue && targetUserId.Value != loggedUserId.Value && (role == "Admin" || role == "Manager"))
            {
                userIdToDisplay = targetUserId.Value;
                isViewingOthers = true;

                var targetUser = await _context.Users
                    .Include(u => u.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userIdToDisplay);

                if (targetUser != null)
                {
                    titleHeader = $"Dostępność: {targetUser.Employee?.FullName ?? targetUser.Username}";
                }
            }

            // *** Pobieramy ustawienie terminu z ContactInfo (singleton settings) ***
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int deadlineDays = contactInfo?.AvailabilityDeadlineDays ?? 7;
            ViewBag.DeadlineDays = deadlineDays;
            // *********************************************************************

            var availabilities = await _context.Availabilities
                .Where(a => a.UserId == userIdToDisplay && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date)
                .ToListAsync();

            var viewModel = new AvailabilityViewModel
            {
                Availabilities = availabilities,
                NewAvailability = new Availability
                {
                    UserId = userIdToDisplay,
                    Date = DateTime.Today,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0)
                }
            };

            // Przekazujemy flagi do widoku, aby wiedział jak się zachować
            ViewBag.IsViewingOthers = isViewingOthers;
            ViewBag.TitleHeader = titleHeader;

            return View(viewModel);
        }

        // POST: /Availability/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Availability newAvailability)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || newAvailability.UserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            ModelState.Remove("NewAvailability.User");

            newAvailability.Date = newAvailability.Date.Date;

            if (newAvailability.EndTime <= newAvailability.StartTime)
                ModelState.AddModelError("NewAvailability.EndTime", "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.");
            if (newAvailability.Date < DateTime.Today)
                ModelState.AddModelError("NewAvailability.Date", "Nie można dodać dostępności dla daty w przeszłości.");
            bool exists = _context.Availabilities.Any(a => a.UserId == userId.Value && a.Date.Date == newAvailability.Date.Date);
            if (exists)
                ModelState.AddModelError("NewAvailability.Date", "Dostępność dla tej daty już istnieje.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Availabilities.Add(newAvailability);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Dodano dostępność na {newAvailability.Date:yyyy-MM-dd}.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Błąd zapisu: {ex.Message}");
                    TempData["ErrorMessage"] = "Błąd zapisu.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Nie udało się dodać dostępności. Sprawdź błędy.";
            }

            var availabilities = _context.Availabilities
               .Where(a => a.UserId == userId.Value && a.Date >= DateTime.Today)
               .OrderBy(a => a.Date)
               .ToList();
            var viewModel = new AvailabilityViewModel { Availabilities = availabilities, NewAvailability = newAvailability };
            return View("Index", viewModel);
        }

        // GET: /Availability/Edit/5
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");
            var availability = _context.Availabilities.FirstOrDefault(a => a.Id == id && a.UserId == userId.Value);
            if (availability == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono dostępności.";
                return RedirectToAction("Index");
            }
            if (availability.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Nie można edytować przeszłych dostępności.";
                return RedirectToAction("Index");
            }
            return View(availability);
        }

        // POST: /Availability/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Availability availability)
        {
            if (id != availability.Id) return BadRequest();

            var userId = HttpContext.Session.GetInt32("UserId");
            var originalAvailability = _context.Availabilities.AsNoTracking().FirstOrDefault(a => a.Id == id && a.UserId == userId.Value);
            if (originalAvailability == null) return RedirectToAction("AccessDenied", "Auth");
            if (originalAvailability.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Nie można edytować przeszłych dostępności.";
                return RedirectToAction("Index");
            }

            ModelState.Remove("User");

            availability.UserId = userId.Value;
            availability.Date = originalAvailability.Date;

            if (availability.EndTime <= availability.StartTime)
                ModelState.AddModelError("EndTime", "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(availability).State = EntityState.Modified;
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Zaktualizowano dostępność na {availability.Date:yyyy-MM-dd}.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Dane zostały zmodyfikowane przez kogoś innego.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Błąd zapisu: {ex.Message}");
                }
            }

            TempData["ErrorMessage"] = "Nie udało się zapisać zmian.";
            return View(availability);
        }

        // POST: /Availability/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");
            var availability = _context.Availabilities.FirstOrDefault(a => a.Id == id && a.UserId == userId.Value);
            if (availability != null)
            {
                if (availability.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Nie można usunąć przeszłych dostępności.";
                }
                else
                {
                    _context.Availabilities.Remove(availability);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Usunięto dostępność na {availability.Date:yyyy-MM-dd}.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Nie znaleziono dostępności.";
            }
            return RedirectToAction("Index");
        }

        // GET: /Availability/Provide
        [HttpGet]
        public IActionResult Provide(int? year, int? month)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            int currentYear = year ?? DateTime.Today.Year;
            int currentMonth = month ?? DateTime.Today.Month;
            var today = DateTime.Today;

            if (currentYear < today.Year || (currentYear == today.Year && currentMonth < today.Month))
            {
                return RedirectToAction("Provide", new { year = today.Year, month = today.Month });
            }

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var availabilities = _context.Availabilities
                .Where(a => a.UserId == userId.Value && a.Date >= startDate && a.Date <= endDate)
                .ToDictionary(a => a.Date.Date, a => a);

            var viewModel = new ProvideAvailabilityViewModel { Year = currentYear, Month = currentMonth };

            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                bool isPastDate = day.Date < today;
                var entry = new MonthEntryViewModel
                {
                    Date = day.Date,
                    IsInCurrentMonth = true,
                    IsSelected = availabilities.ContainsKey(day.Date) && !isPastDate
                };

                if (availabilities.TryGetValue(day.Date, out var availability))
                {
                    entry.Availability = availability;
                }
                else
                {
                    if (!isPastDate)
                    {
                        entry.Availability = new Availability
                        {
                            UserId = userId.Value,
                            Date = day.Date,
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0)
                        };
                    }
                    else
                    {
                        entry.Availability = null;
                    }
                }
                viewModel.Days.Add(entry);
            }
            return View(viewModel);
        }

        // POST: /Availability/Provide (nawigacja)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Provide(ProvideAvailabilityViewModel model, string direction)
        {
            if (!string.IsNullOrEmpty(direction))
            {
                var date = new DateTime(model.Year, model.Month, 1);
                if (direction == "prev") date = date.AddMonths(-1);
                else if (direction == "next") date = date.AddMonths(1);

                if (date.Year < DateTime.Today.Year || (date.Year == DateTime.Today.Year && date.Month < DateTime.Today.Month))
                    date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                return RedirectToAction("Provide", new { year = date.Year, month = date.Month });
            }

            TempData["ErrorMessage"] = "Wystąpił błąd.";
            return View(model);
        }

        // POST: /Availability/SubmitAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitAvailability(ProvideAvailabilityViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            for (int i = 0; i < model.Days.Count; i++)
            {
                ModelState.Remove($"Days[{i}].Availability.User");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Popraw błędy w formularzu.";
                return View("Provide", model);
            }

            try
            {
                var existingAvailabilities = _context.Availabilities
                    .Where(a => a.UserId == userId.Value && a.Date.Year == model.Year && a.Date.Month == model.Month)
                    .ToList();

                foreach (var day in model.Days)
                {
                    if (day.Date < DateTime.Today) continue;

                    var existing = existingAvailabilities.FirstOrDefault(a => a.Date.Date == day.Date.Date);

                    if (day.IsSelected)
                    {
                        if (day.Availability == null) continue;

                        if (existing != null)
                        {
                            existing.StartTime = day.Availability.StartTime;
                            existing.EndTime = day.Availability.EndTime;
                            _context.Entry(existing).State = EntityState.Modified;
                        }
                        else
                        {
                            var newAvail = new Availability
                            {
                                UserId = userId.Value,
                                Date = day.Date.Date,
                                StartTime = day.Availability.StartTime,
                                EndTime = day.Availability.EndTime
                            };
                            _context.Availabilities.Add(newAvail);
                        }
                    }
                    else
                    {
                        if (existing != null)
                        {
                            _context.Availabilities.Remove(existing);
                        }
                    }
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Dostępność została zapisana.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Błąd zapisu: {ex.Message}";
                return View("Provide", model);
            }

            return RedirectToAction("Provide", new { year = model.Year, month = model.Month });
        }

        // --- ZARZĄDZANIE ---

        // GET: Availability/Manage
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage()
        {
            var today = DateTime.Today;
            var targetDate = today.AddMonths(1);
            int targetMonth = targetDate.Month;
            int targetYear = targetDate.Year;

            // Pobranie ustawienia terminu z ContactInfo
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int deadlineDays = contactInfo?.AvailabilityDeadlineDays ?? 7;
            ViewBag.DeadlineDays = deadlineDays;

            int daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
            // Obliczamy ile dni zostało do końca miesiąca
            int daysRemaining = daysInCurrentMonth - today.Day;

            // "Pilne" jeśli zostało mniej lub tyle samo dni co w ustawieniu
            bool isUrgent = daysRemaining <= deadlineDays;

            var employees = await _context.Users
                .Where(u => u.Role == "Employee")
                .Include(u => u.Employee)
                .ToListAsync();

            var employeesWithAvailability = await _context.Availabilities
                .Where(a => a.Date.Month == targetMonth && a.Date.Year == targetYear)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var statusList = employees.Select(u => new EmployeeAvailabilityStatusViewModel
            {
                UserId = u.Id,
                FullName = u.Employee?.FullName ?? u.Username,
                Email = u.Email,
                Phone = u.Employee?.Phone ?? u.PhoneNumber ?? "-",
                HasProvidedAvailability = employeesWithAvailability.Contains(u.Id)
            }).OrderBy(x => x.HasProvidedAvailability).ThenBy(x => x.FullName).ToList();

            var vm = new AvailabilityManageViewModel
            {
                NextMonth = targetMonth,
                NextMonthYear = targetYear,
                IsUrgent = isUrgent,
                EmployeesStatus = statusList
            };

            return View(vm);
        }

        // POST: Availability/UpdateDeadline
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> UpdateDeadline(int deadlineDays)
        {
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contactInfo == null)
            {
                // Jeśli z jakiegoś powodu nie ma rekordu (choć DatabaseSeeder powinien go dodać), tworzymy go
                contactInfo = new ContactInfo
                {
                    AddressCity = "Brak",
                    AddressStreet = "Brak",
                    AddressZipCode = "00-000",
                    ContactEmail = "brak@brak.pl",
                    PhoneNumber = "000000000"
                };
                _context.ContactInfos.Add(contactInfo);
            }

            if (deadlineDays < 1 || deadlineDays > 31)
            {
                TempData["ErrorMessage"] = "Termin musi wynosić od 1 do 31 dni.";
                return RedirectToAction(nameof(Manage));
            }

            contactInfo.AvailabilityDeadlineDays = deadlineDays;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zaktualizowano ustawienia terminu zgłaszania dyspozycyjności.";
            return RedirectToAction(nameof(Manage));
        }
    }
}