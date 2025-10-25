using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Filters;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class AvailabilityController : Controller
    {
        private readonly AppDbContext _ctx;
        public AvailabilityController(AppDbContext ctx) => _ctx = ctx;

        /*-------------------------------------------------
         * 1. LISTA MIESIĘCY  (/Availability)
        -------------------------------------------------*/
        public IActionResult Index(int? year)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return RedirectToAction("Login", "Auth");

            var data = _ctx.Availabilities.Where(a => a.UserId == uid.Value);

            if (year.HasValue)
                data = data.Where(a => a.Date.Year == year.Value);

            var months = data
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .Select(g => new MonthEntryViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            ViewBag.SelectedYear = year;
            ViewBag.Years = _ctx.Availabilities
                                .Where(a => a.UserId == uid.Value)
                                .Select(a => a.Date.Year)
                                .OrderByDescending(y => y)
                                .Distinct()
                                .ToList();

            return View(months);
        }

        /*-------------------------------------------------
         * 2. PROVIDE  –  formularz wyboru
        -------------------------------------------------*/
        [HttpGet]
        public IActionResult Provide()
        {
            var next = DateTime.Now.AddMonths(1);
            return View(new ProvideAvailabilityViewModel
            {
                Year = next.Year,
                Month = next.Month
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Provide(ProvideAvailabilityViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return RedirectToAction("Login", "Auth");

            bool exists = _ctx.Availabilities.Any(a =>
                a.UserId == uid.Value &&
                a.Date.Year == vm.Year &&
                a.Date.Month == vm.Month);

            if (exists)
            {
                ModelState.AddModelError(string.Empty,
                    "Availability for this month already exists. You can edit it from the list.");
                return View(vm);
            }

            return RedirectToAction("Edit", new { year = vm.Year, month = vm.Month });
        }

        /*-------------------------------------------------
         * 3. EDIT  –  kalendarz
        -------------------------------------------------*/
        [HttpGet]
        public IActionResult Edit(int year, int month)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return RedirectToAction("Login", "Auth");

            int days = DateTime.DaysInMonth(year, month);

            var existing = _ctx.Availabilities
                .Where(a => a.UserId == uid.Value &&
                            a.Date.Year == year &&
                            a.Date.Month == month)
                .ToList();

            var vm = new AvailabilityViewModel
            {
                Year = year,
                Month = month
            };

            for (int d = 1; d <= days; d++)
            {
                var rec = existing.FirstOrDefault(e => e.Date.Day == d);
                vm.Days.Add(new DayAvailabilityViewModel
                {
                    Day = d,
                    IsAvailable = rec != null,
                    StartTime = rec?.StartTime,
                    EndTime = rec?.EndTime
                });
            }

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(AvailabilityViewModel vm)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return RedirectToAction("Login", "Auth");

            /* 1️⃣  Usuń stare wpisy i zapisz zmianę */
            var old = _ctx.Availabilities
                .Where(a => a.UserId == uid.Value &&
                            a.Date.Year == vm.Year &&
                            a.Date.Month == vm.Month);
            _ctx.Availabilities.RemoveRange(old);
            _ctx.SaveChanges();

            /* 2️⃣  Dodaj nowe wybrane dni */
            foreach (var d in vm.Days.Where(d => d.IsAvailable))
            {
                _ctx.Availabilities.Add(new Availability
                {
                    UserId = uid.Value,
                    Date = new DateTime(vm.Year, vm.Month, d.Day),
                    StartTime = d.StartTime ?? new TimeSpan(0, 0, 0),
                    EndTime = d.EndTime ?? new TimeSpan(23, 59, 0)
                });
            }
            _ctx.SaveChanges();

            TempData["Success"] = "Availability saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
