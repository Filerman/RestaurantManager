using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using RestaurantManager.ViewModels;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Globalization;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager", "Employee")]
    public class SchedulesController : Controller
    {
        private readonly AppDbContext _context;

        public SchedulesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Schedules (Lista grafików)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
            return View(schedules);
        }

        // GET: Schedules/Create
        [RoleAuthorize("Admin", "Manager")]
        public IActionResult Create()
        {
            var viewModel = new ScheduleCreateViewModel();
            return View(viewModel);
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Create(ScheduleCreateViewModel viewModel)
        {
            bool overlap = await _context.Schedules.AnyAsync(s =>
                (viewModel.StartDate >= s.StartDate && viewModel.StartDate <= s.EndDate) ||
                (viewModel.EndDate >= s.StartDate && viewModel.EndDate <= s.EndDate) ||
                (viewModel.StartDate < s.StartDate && viewModel.EndDate > s.EndDate)
            );

            if (overlap)
            {
                ModelState.AddModelError("", "Istnieje już grafik pokrywający się z wybranym zakresem dat.");
            }
            var validationResults = viewModel.Validate(new ValidationContext(viewModel));
            foreach (var validationResult in validationResults)
            {
                if (validationResult.MemberNames.Any())
                {
                    foreach (var memberName in validationResult.MemberNames)
                    {
                        ModelState.AddModelError(memberName ?? string.Empty, validationResult.ErrorMessage ?? "Validation error");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, validationResult.ErrorMessage ?? "Validation error");
                }
            }

            if (ModelState.IsValid)
            {
                var newSchedule = new Schedule
                {
                    StartDate = viewModel.StartDate.Date,
                    EndDate = viewModel.EndDate.Date,
                    IsPublished = false,
                    CreatedAt = DateTime.Now
                };
                _context.Add(newSchedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Utworzono nowy grafik. Teraz możesz dodać zmiany.";
                return RedirectToAction(nameof(Edit), new { id = newSchedule.Id });
            }
            return View(viewModel);
        }

        // GET: Schedules/Edit/5 (Edytor grafiku)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            Console.WriteLine($"--->>> GET Edit: Attempting to load Schedule with ID: {id}");

            // 1. Pobieramy grafik WRAZ ze zmianami
            var schedule = await _context.Schedules
                .Include(s => s.Shifts)
                    .ThenInclude(sh => sh.EmployeeUser)
                        .ThenInclude(u => u.Employee)
                .Include(s => s.Shifts)
                    .ThenInclude(sh => sh.ShiftPositionTag)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                Console.WriteLine($"--->>> GET Edit: Schedule with ID: {id} not found.");
                return NotFound();
            }
            Console.WriteLine($"--->>> GET Edit: Schedule found: ID={schedule.Id}");

            // --- Formatowanie listy pracowników (Staż + Godziny) ---

            var employees = await _context.Employees.ToListAsync();

            var employeesWithInfo = employees.Select(e =>
            {
                double hoursInSchedule = 0;
                if (schedule.Shifts != null)
                {
                    hoursInSchedule = schedule.Shifts
                        .Where(s => s.UserId == e.UserId)
                        .Sum(s => (s.EndTime - s.StartTime).TotalHours);
                }

                string seniority = GetSeniorityString(e.HireDate);

                return new
                {
                    UserId = e.UserId,
                    DisplayText = $"{e.FullName} (Staż: {seniority} | Graf: {hoursInSchedule:F1}h)"
                };
            }).OrderBy(x => x.DisplayText).ToList();

            ViewData["Employees"] = new SelectList(employeesWithInfo, "UserId", "DisplayText");
            ViewData["PositionTags"] = new SelectList(await _context.PositionTags.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");

            var viewModel = new ScheduleEditViewModel
            {
                Id = schedule.Id,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                IsPublished = schedule.IsPublished,
                AvailableTags = await _context.PositionTags.OrderBy(t => t.Name).ToListAsync()
            };

            var shiftsByDate = schedule.Shifts
                .OrderBy(sh => sh.StartTime)
                .GroupBy(sh => sh.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (DateTime date = schedule.StartDate; date <= schedule.EndDate; date = date.AddDays(1))
            {
                var dayViewModel = new ScheduleDayViewModel { Date = date, DayName = date.ToString("dddd", new System.Globalization.CultureInfo("pl-PL")) };
                if (shiftsByDate.TryGetValue(date, out var shiftsForDay))
                {
                    dayViewModel.Shifts = shiftsForDay.Select(sh => new ShiftViewModel
                    {
                        Id = sh.Id,
                        ScheduleId = sh.ScheduleId,
                        Date = sh.Date,
                        StartTime = sh.StartTime,
                        EndTime = sh.EndTime,
                        PositionTagId = sh.PositionTagId,
                        PositionTagName = sh.ShiftPositionTag?.Name,
                        AssignedUserId = sh.UserId,
                        AssignedUserName = sh.EmployeeUser?.Employee?.FullName ?? sh.EmployeeUser?.Username ?? "Nieprzypisany"
                    }).ToList();
                }
                viewModel.Days.Add(dayViewModel);
            }

            try
            {
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--->>> !!! EXCEPTION during GET Edit View() call: {ex.ToString()}");
                return Content($"Error loading schedule editor: {ex.Message}. Check application logs.");
            }
        }

        // POST: Schedules/Edit/5 (Zapisuje tylko status publikacji)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id, IsPublished")] Schedule scheduleUpdate)
        {
            if (id != scheduleUpdate.Id) return NotFound();
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();
            schedule.IsPublished = scheduleUpdate.IsPublished;
            try
            {
                _context.Update(schedule); await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = schedule.IsPublished ? "Grafik został opublikowany." : "Grafik został oznaczony jako roboczy.";
            }
            catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "Wystąpił konflikt współbieżności."; }
            catch (Exception ex) { TempData["ErrorMessage"] = $"Wystąpił błąd: {ex.Message}"; }
            return RedirectToAction(nameof(Edit), new { id = id });
        }

        // GET: Schedules/Delete/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var schedule = await _context.Schedules.FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null) return NotFound();
            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null) { _context.Schedules.Remove(schedule); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Grafik został usunięty."; }
            return RedirectToAction(nameof(Index));
        }

        // GET: Schedules/MySchedule
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> MySchedule(int? year, int? month, string viewType = "calendar")
        {
            var userId = GetUserIdFromSession();
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            // Domyślnie bieżący miesiąc i rok
            int currentYear = year ?? DateTime.Today.Year;
            int currentMonth = month ?? DateTime.Today.Month;

            // Obsługa "przekręcania" roku
            if (currentMonth > 12) { currentMonth = 1; currentYear++; }
            else if (currentMonth < 1) { currentMonth = 12; currentYear--; }

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Pobieramy zmiany
            var myShifts = await _context.Shifts
                .Include(s => s.Schedule)
                .Include(s => s.ShiftPositionTag)
                .Where(s => s.UserId == userId.Value
                            && s.Date >= startDate
                            && s.Date <= endDate
                            && s.Schedule.IsPublished)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.CurrentYear = currentYear;
            ViewBag.CurrentMonth = currentMonth;
            ViewBag.MonthName = startDate.ToString("MMMM yyyy", new CultureInfo("pl-PL"));

            // Przekazujemy wybrany typ widoku (domyślnie "calendar")
            ViewBag.ViewType = viewType;

            return View(myShifts);
        }


        // GET: /Schedules/GetAvailableEmployees
        [HttpGet]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<JsonResult> GetAvailableEmployees(DateTime date, TimeSpan startTime, TimeSpan endTime, int positionTagId)
        {
            if (positionTagId <= 0 || endTime <= startTime) return Json(new List<object>());
            try
            {
                var availableUserIds = await _context.Availabilities
                    .Where(a => a.Date.Date == date.Date && a.StartTime <= startTime && a.EndTime >= endTime)
                    .Select(a => a.UserId).Distinct().ToListAsync();

                if (!availableUserIds.Any()) return Json(new List<object>());

                var availableUsers = await _context.Users
                    .Where(u => availableUserIds.Contains(u.Id))
                    .Include(u => u.Employee).ThenInclude(e => e.PositionTags)
                    .ToListAsync();

                var schedule = await _context.Schedules
                    .Include(s => s.Shifts)
                    .FirstOrDefaultAsync(s => date >= s.StartDate && date <= s.EndDate);

                var filteredEmployees = availableUsers
                     .Where(u => u.Role == "Admin" || u.Role == "Manager" || (u.Employee != null && u.Employee.PositionTags.Any(pt => pt.Id == positionTagId)))
                     .Select(u => {
                         double hours = 0;
                         string seniority = "-";

                         if (u.Employee != null)
                         {
                             seniority = GetSeniorityString(u.Employee.HireDate);
                             if (schedule != null && schedule.Shifts != null)
                             {
                                 hours = schedule.Shifts
                                    .Where(s => s.UserId == u.Id)
                                    .Sum(s => (s.EndTime - s.StartTime).TotalHours);
                             }
                         }

                         return new
                         {
                             u.Id,
                             DisplayName = u.Employee != null
                                ? $"{u.Employee.FullName} (Staż: {seniority} | Graf: {hours:F1}h)"
                                : u.Username
                         };
                     })
                     .OrderBy(u => u.DisplayName).ToList();

                return Json(filteredEmployees);
            }
            catch (Exception ex) { Console.WriteLine($"--->>> Error in GetAvailableEmployees: {ex.ToString()}"); return Json(new List<object>()); }
        }

        // GET: /Schedules/GetShiftDetails
        [HttpGet]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<JsonResult> GetShiftDetails(int shiftId)
        {
            if (shiftId <= 0) return Json(new { error = "Nieprawidłowe ID zmiany." });
            var shift = await _context.Shifts.Where(sh => sh.Id == shiftId)
                .Select(sh => new {
                    sh.Id,
                    sh.ScheduleId,
                    Date = sh.Date.ToString("yyyy-MM-dd"),
                    StartTime = sh.StartTime.ToString(@"hh\:mm"),
                    EndTime = sh.EndTime.ToString(@"hh\:mm"),
                    sh.PositionTagId,
                    UserId = sh.UserId
                }).FirstOrDefaultAsync();
            if (shift == null) return Json(new { error = "Nie znaleziono zmiany." });
            return Json(shift);
        }

        // POST: /Schedules/SaveShift
        [HttpPost]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<JsonResult> SaveShift(ShiftViewModel model)
        {
            Console.WriteLine($"--->>> POST SaveShift received model: {JsonSerializer.Serialize(model)}");

            if (model.ScheduleId <= 0 || model.PositionTagId == null || model.PositionTagId <= 0)
            {
                return Json(new { success = false, message = "Nieprawidłowe dane wejściowe (ID grafiku lub tag)." });
            }
            if (model.EndTime <= model.StartTime)
            {
                return Json(new { success = false, message = "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia." });
            }

            var schedule = await _context.Schedules.FindAsync(model.ScheduleId);
            if (schedule == null)
            {
                return Json(new { success = false, message = "Nie znaleziono grafiku." });
            }
            if (model.Date.Date < schedule.StartDate.Date || model.Date.Date > schedule.EndDate.Date)
            {
                return Json(new { success = false, message = "Data zmiany wykracza poza zakres grafiku." });
            }

            int? assignedUserId = model.AssignedUserId.HasValue && model.AssignedUserId > 0 ? model.AssignedUserId : null;

            if (assignedUserId.HasValue)
            {
                bool hasConflict = await _context.Shifts
                    .AnyAsync(s => s.Id != model.Id && s.UserId == assignedUserId.Value && s.Date.Date == model.Date.Date && s.StartTime < model.EndTime && s.EndTime > model.StartTime);
                if (hasConflict) { return Json(new { success = false, message = "Wybrany pracownik ma już inną zmianę kolidującą z tymi godzinami." }); }

                bool isAvailable = await _context.Availabilities.AnyAsync(a => a.UserId == assignedUserId.Value && a.Date.Date == model.Date.Date && a.StartTime <= model.StartTime && a.EndTime >= model.EndTime);
                if (!isAvailable) { return Json(new { success = false, message = "Wybrany pracownik nie jest dostępny w tych godzinach." }); }

                bool hasTag = await _context.Users
                    .Where(u => u.Id == assignedUserId.Value && (u.Role == "Admin" || u.Role == "Manager" || (u.Employee != null && u.Employee.PositionTags.Any(pt => pt.Id == model.PositionTagId))))
                    .AnyAsync();
                if (!hasTag) { return Json(new { success = false, message = "Wybrany pracownik nie posiada wymaganego tagu stanowiska." }); }
            }

            Shift shiftEntity;
            bool isNew = model.Id == 0;

            if (isNew)
            {
                shiftEntity = new Shift { ScheduleId = model.ScheduleId, Date = model.Date.Date, StartTime = model.StartTime, EndTime = model.EndTime, PositionTagId = model.PositionTagId, UserId = assignedUserId };
                _context.Shifts.Add(shiftEntity);
            }
            else
            {
                shiftEntity = await _context.Shifts.FindAsync(model.Id);
                if (shiftEntity == null || shiftEntity.ScheduleId != model.ScheduleId) { return Json(new { success = false, message = "Nie znaleziono zmiany do aktualizacji." }); }
                shiftEntity.StartTime = model.StartTime; shiftEntity.EndTime = model.EndTime;
                shiftEntity.PositionTagId = model.PositionTagId; shiftEntity.UserId = assignedUserId;
                _context.Entry(shiftEntity).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
                string assignedUserName = "Nieprzypisany";
                if (shiftEntity.UserId.HasValue) { assignedUserName = (await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == shiftEntity.UserId.Value))?.FullName ?? (await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shiftEntity.UserId.Value))?.Username ?? "Błąd"; }
                string positionTagName = (await _context.PositionTags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == shiftEntity.PositionTagId))?.Name ?? "Brak tagu";
                return Json(new { success = true, shift = new { id = shiftEntity.Id, date = shiftEntity.Date.ToString("yyyy-MM-dd"), startTime = shiftEntity.StartTime.ToString(@"hh\:mm"), endTime = shiftEntity.EndTime.ToString(@"hh\:mm"), positionTagId = shiftEntity.PositionTagId, positionTagName = positionTagName, assignedUserId = shiftEntity.UserId, assignedUserName = assignedUserName } });
            }
            catch (Exception ex) { Console.WriteLine($"--->>> !!! Exception saving shift: {ex.ToString()}"); return Json(new { success = false, message = "Nieoczekiwany błąd serwera." }); }
        }

        // POST: /Schedules/DeleteShift
        [HttpPost]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<JsonResult> DeleteShift(int shiftId)
        {
            if (shiftId <= 0) return Json(new { success = false, message = "Nieprawidłowe ID zmiany." });
            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return Json(new { success = true });
            try { _context.Shifts.Remove(shift); await _context.SaveChangesAsync(); return Json(new { success = true }); }
            catch (Exception ex) { Console.WriteLine($"--->>> !!! Error deleting shift: {ex.ToString()}"); return Json(new { success = false, message = "Wystąpił błąd serwera." }); }
        }

        // --- METODY POMOCNICZE ---
        private int? GetUserIdFromSession() { return HttpContext.Session.GetInt32("UserId"); }

        private string GetSeniorityString(DateTime hireDate)
        {
            if (hireDate == DateTime.MinValue) return "-";
            var today = DateTime.Now;
            int months = ((today.Year - hireDate.Year) * 12) + today.Month - hireDate.Month;
            if (today.Day < hireDate.Day) months--;

            if (months < 0) return "0 dni";
            if (months < 1) return $"{(today - hireDate).Days} dni";

            int years = months / 12;
            int remainingMonths = months % 12;

            if (years > 0) return $"{years} lat, {remainingMonths} mies.";
            return $"{months} mies.";
        }
    }
}