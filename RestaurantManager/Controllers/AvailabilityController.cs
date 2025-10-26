using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RestaurantManager.Filters;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var availabilities = _context.Availabilities
                .Where(a => a.UserId == userId.Value && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date)
                .ToList();

            var viewModel = new AvailabilityViewModel
            {
                Availabilities = availabilities,
                NewAvailability = new Availability
                {
                    UserId = userId.Value,
                    Date = DateTime.Today,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0)
                }
            };

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

            // POPRAWKA: Usuwamy obiekt User z walidacji
            ModelState.Remove("NewAvailability.User");
            // Można też usunąć globalnie dla Availability, ale lepiej precyzyjnie
            // ModelState.Remove("User");

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

            // Przygotuj ViewModel i zwróć widok Index z błędami
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

            // POPRAWKA: Usuwamy obiekt User z walidacji
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
                        entry.Availability = null; // Zostaw null dla przeszłych dni bez wpisu
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

            // POPRAWKA: Ręczne usunięcie błędów dla obiektów User z listy Days.Availability
            // Walidator IValidatableObject i tak sprawdzi godziny.
            for (int i = 0; i < model.Days.Count; i++)
            {
                ModelState.Remove($"Days[{i}].Availability.User");
            }
            // Można też spróbować ogólnie: ModelState.Remove("Availability.User");


            if (!ModelState.IsValid) // To sprawdzi teraz głównie walidację z IValidatableObject
            {
                TempData["ErrorMessage"] = "Popraw błędy w formularzu.";
                return View("Provide", model); // Zwróć widok z błędami
            }

            // --- Logika Zapisywania ---
            try
            {
                var existingAvailabilities = _context.Availabilities
                    .Where(a => a.UserId == userId.Value && a.Date.Year == model.Year && a.Date.Month == model.Month)
                    .ToList();

                foreach (var day in model.Days)
                {
                    if (day.Date < DateTime.Today) continue; // Ignoruj przeszłe dni

                    var existing = existingAvailabilities.FirstOrDefault(a => a.Date.Date == day.Date.Date);

                    if (day.IsSelected)
                    {
                        if (day.Availability == null) continue; // Pomiń, jeśli brak danych

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
                return View("Provide", model); // Zwróć widok z błędem, zachowując dane
            }

            return RedirectToAction("Provide", new { year = model.Year, month = model.Month });
        }
    }
}