using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace RestaurantManager.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _ctx;
        public ReservationsController(AppDbContext ctx) => _ctx = ctx;

        // GET: /Reservations
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            IQueryable<Reservation> query = _ctx.Reservations
                .Include(r => r.Table)
                .Include(r => r.User);

            if (userRole == "Guest")
            {
                query = query.Where(r => r.UserId == userId.Value);
            }

            var list = query.OrderByDescending(r => r.DateTime).ToList();
            return View(list);
        }

        // GET: /Reservations/Create
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            PopulateTablesDropDownList();

            var user = _ctx.Users.Find(userId.Value);

            var model = new Reservation();
            if (user != null)
            {
                model.CustomerEmail = user.Email;
            }

            return View(model);
        }

        // POST: /Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Reservation r)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            r.UserId = userId.Value;
            r.Status = ReservationStatus.Pending;

            ModelState.Remove(nameof(r.User));
            ModelState.Remove(nameof(r.Table));

            if (r.DateTime < DateTime.Now)
            {
                ModelState.AddModelError("DateTime", "Nie można dokonać rezerwacji w przeszłości.");
            }

            var dayOfWeek = r.DateTime.DayOfWeek;
            var openingHour = _ctx.OpeningHours.FirstOrDefault(oh => oh.DayOfWeek == dayOfWeek);

            if (openingHour != null)
            {
                if (openingHour.IsClosed)
                {
                    ModelState.AddModelError("DateTime", "W tym dniu restauracja jest nieczynna.");
                }
                else
                {
                    var time = r.DateTime.TimeOfDay;
                    if (time < openingHour.OpenTime || time > openingHour.CloseTime)
                    {
                        ModelState.AddModelError("DateTime", $"Restauracja jest czynna w godzinach {openingHour.OpenTime:hh\\:mm} - {openingHour.CloseTime:hh\\:mm}.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var settings = _ctx.ContactInfos.FirstOrDefault();
                int occupancyMinutes = settings?.DefaultTableOccupancyMinutes ?? 120;

                var reservationDuration = TimeSpan.FromMinutes(occupancyMinutes);
                var newReservationStart = r.DateTime;
                var newReservationEnd = r.DateTime.Add(reservationDuration);

                var conflictThreshold = newReservationStart.Subtract(reservationDuration);

                bool overlap = _ctx.Reservations
                    .Any(existing =>
                        existing.TableId == r.TableId &&
                        existing.Status != ReservationStatus.Rejected &&
                        existing.DateTime < newReservationEnd &&
                        existing.DateTime > conflictThreshold
                    );

                if (overlap)
                {
                    ModelState.AddModelError("TableId", $"Ten stolik jest już zarezerwowany w tym czasie (zakładany czas wizyty: {occupancyMinutes} min). Wybierz inną godzinę lub stolik.");
                }
            }

            if (!ModelState.IsValid)
            {
                PopulateTablesDropDownList(r.TableId);
                return View(r);
            }

            _ctx.Reservations.Add(r);
            _ctx.SaveChanges();

            TempData["SuccessMessage"] = "Rezerwacja złożona. Oczekuj na potwierdzenie przez obsługę.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reservations/Edit/5
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Edit(int id)
        {
            var r = _ctx.Reservations.Find(id);
            if (r == null) return NotFound();

            PopulateTablesDropDownList(r.TableId);
            return View(r);
        }

        // POST: /Reservations/Edit
        [HttpPost]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Reservation r)
        {
            var original = _ctx.Reservations.AsNoTracking().FirstOrDefault(x => x.Id == r.Id);
            if (original == null) return NotFound();

            r.UserId = original.UserId;

            ModelState.Remove(nameof(r.User));
            ModelState.Remove(nameof(r.Table));

            if (!ModelState.IsValid)
            {
                PopulateTablesDropDownList(r.TableId);
                return View(r);
            }

            _ctx.Reservations.Update(r);
            _ctx.SaveChanges();
            TempData["SuccessMessage"] = "Zaktualizowano rezerwację.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reservations/Delete/5
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Delete(int id)
        {
            var r = _ctx.Reservations
                        .Include(r => r.Table)
                        .FirstOrDefault(r => r.Id == id);

            if (r == null) return NotFound();
            return View(r);
        }

        // POST: /Reservations/Delete
        [HttpPost, ActionName("Delete")]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var r = _ctx.Reservations.Find(id);
            if (r != null)
            {
                _ctx.Reservations.Remove(r);
                _ctx.SaveChanges();
                TempData["SuccessMessage"] = "Usunięto rezerwację.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateTablesDropDownList(object selectedTable = null)
        {
            var tablesQuery = from t in _ctx.Tables
                              where t.IsAvailable == true
                              orderby t.Name
                              select t;

            var tableListItems = tablesQuery.AsNoTracking().Select(t => new
            {
                t.Id,
                DisplayText = $"{t.Name} (Pojemność: {t.Capacity})"
            });

            ViewBag.TableId = new SelectList(tableListItems, "Id", "DisplayText", selectedTable);
        }
    }
}