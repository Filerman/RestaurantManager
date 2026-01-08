using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Do SelectListItem
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace RestaurantManager.Controllers
{
    public class LossLogsController : Controller
    {
        private readonly AppDbContext _context;

        public LossLogsController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /LossLogs/Index - Rejestr strat z filtrowaniem
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.LossLogs
                .Include(l => l.ReportedByUser)
                .Include(l => l.MenuItem) // Dołączamy info o menu
                .AsQueryable();

            // Domyślne filtrowanie (np. bieżący miesiąc, jeśli brak dat)
            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            query = query.Where(l => l.DateReported >= startDate && l.DateReported <= endDate);

            var lossLogs = await query.OrderByDescending(l => l.DateReported).ToListAsync();

            // Przekazujemy daty do widoku, żeby zachować stan filtrów
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(lossLogs);
        }

        // [GET] /LossLogs/Report - Formularz zgłaszania straty
        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Report()
        {
            // Pobieramy listę dań do selecta
            ViewBag.MenuItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.Name} ({m.Price:F2} zł)"
                })
                .ToListAsync();

            var model = new LossLog
            {
                DateReported = DateTime.Now,
                Quantity = 1 // Domyślna ilość
            };
            return View(model);
        }

        // [POST] /LossLogs/Report - Zapisanie straty
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Report(LossLog lossLog, string itemSource)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 1. Walidacja "Menu" vs "Własne" (itemSource pochodzi z radio buttonów w widoku)
            if (itemSource == "menu")
            {
                if (lossLog.MenuItemId == null)
                    ModelState.AddModelError("MenuItemId", "Wybierz pozycję z menu.");

                lossLog.CustomItemName = null;
                lossLog.CustomItemCost = null;
            }
            else // itemSource == "custom"
            {
                if (string.IsNullOrWhiteSpace(lossLog.CustomItemName))
                    ModelState.AddModelError("CustomItemName", "Wpisz nazwę przedmiotu.");
                if (lossLog.CustomItemCost == null)
                    ModelState.AddModelError("CustomItemCost", "Podaj koszt jednostkowy.");

                lossLog.MenuItemId = null;
            }

            // 2. Uzupełnianie danych systemowych
            lossLog.ReportedByUserId = userId.Value;
            lossLog.DateReported = DateTime.Now;

            // Ignorujemy walidację nawigacji, bo ustawiamy ID ręcznie
            ModelState.Remove(nameof(lossLog.ReportedByUser));
            ModelState.Remove(nameof(lossLog.MenuItem));
            // Ignorujemy EstimatedValue, bo zaraz ją sami policzymy
            ModelState.Remove(nameof(lossLog.EstimatedValue));

            if (ModelState.IsValid)
            {
                // 3. Obliczenie wartości straty (EstimatedValue)
                if (lossLog.MenuItemId != null)
                {
                    var menuItem = await _context.MenuItems.FindAsync(lossLog.MenuItemId);
                    if (menuItem != null)
                    {
                        lossLog.EstimatedValue = menuItem.Price * lossLog.Quantity;
                    }
                }
                else
                {
                    lossLog.EstimatedValue = (lossLog.CustomItemCost ?? 0) * lossLog.Quantity;
                }

                _context.LossLogs.Add(lossLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Strata została pomyślnie zarejestrowana.";
                return RedirectToAction(nameof(Report));
            }

            // W razie błędu, ponownie ładujemy listę dań
            ViewBag.MenuItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.Name} ({m.Price:F2} zł)"
                })
                .ToListAsync();

            return View(lossLog);
        }

        // [POST] /LossLogs/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var lossLog = await _context.LossLogs.FindAsync(id);
            if (lossLog == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono wpisu.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.LossLogs.Remove(lossLog);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Wpis straty został pomyślnie usunięty.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania wpisu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}