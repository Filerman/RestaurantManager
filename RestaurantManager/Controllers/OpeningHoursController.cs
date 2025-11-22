using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Collections.Generic;
using System;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")]
    public class OpeningHoursController : Controller
    {
        private readonly AppDbContext _context;

        public OpeningHoursController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /OpeningHours
        public IActionResult Index()
        {
            // Sortowanie: Poniedziałek (1) ... Niedziela (7)
            var hours = _context.OpeningHours.ToList()
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToList();

            return View(hours);
        }

        // POST: /OpeningHours/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(List<OpeningHour> hours)
        {
            if (hours == null || !hours.Any()) return RedirectToAction(nameof(Index));

            foreach (var item in hours)
            {
                var dbEntry = _context.OpeningHours.Find(item.Id);
                if (dbEntry != null)
                {
                    dbEntry.OpenTime = item.OpenTime;
                    dbEntry.CloseTime = item.CloseTime;
                    dbEntry.IsClosed = item.IsClosed;
                }
            }
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Godziny otwarcia zostały zaktualizowane.";
            return RedirectToAction(nameof(Index));
        }
    }
}