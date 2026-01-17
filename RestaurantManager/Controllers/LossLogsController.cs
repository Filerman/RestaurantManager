using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: /LossLogs/Index
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.LossLogs
                .Include(l => l.ReportedByUser)
                .Include(l => l.MenuItem)
                .AsQueryable();

            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            query = query.Where(l => l.DateReported >= startDate && l.DateReported <= endDate);

            var lossLogs = await query.OrderByDescending(l => l.DateReported).ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(lossLogs);
        }

        // GET: /LossLogs/Report
        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Report()
        {
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
                Quantity = 1
            };
            return View(model);
        }

        // POST: /LossLogs/Report
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

            if (itemSource == "menu")
            {
                if (lossLog.MenuItemId == null)
                    ModelState.AddModelError("MenuItemId", "Wybierz pozycję z menu.");

                lossLog.CustomItemName = null;
                lossLog.CustomItemCost = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(lossLog.CustomItemName))
                    ModelState.AddModelError("CustomItemName", "Wpisz nazwę przedmiotu.");
                if (lossLog.CustomItemCost == null)
                    ModelState.AddModelError("CustomItemCost", "Podaj koszt jednostkowy.");

                lossLog.MenuItemId = null;
            }

            lossLog.ReportedByUserId = userId.Value;
            lossLog.DateReported = DateTime.Now;

            ModelState.Remove(nameof(lossLog.ReportedByUser));
            ModelState.Remove(nameof(lossLog.MenuItem));
            ModelState.Remove(nameof(lossLog.EstimatedValue));

            if (ModelState.IsValid)
            {
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

        // POST: /LossLogs/Delete
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