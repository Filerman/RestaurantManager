using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;

namespace RestaurantManager.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _ctx;
        public ReservationsController(AppDbContext ctx) => _ctx = ctx;

        // GET: /Reservations
        public IActionResult Index()
        {
            // tylko zalogowani mogą zobaczyć listę
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Auth");

            var list = _ctx.Reservations
                           .OrderBy(r => r.DateTime)
                           .ToList();
            return View(list);
        }

        // GET: /Reservations/Create
        public IActionResult Create()
        {
            // tylko zalogowani mogą tworzyć
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST: /Reservations/Create
        [HttpPost]
        public IActionResult Create(Reservation r)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(r);

            _ctx.Reservations.Add(r);
            _ctx.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reservations/Edit/5
        // tylko Employee, Manager i Admin mogą edytować
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Edit(int id)
        {
            var r = _ctx.Reservations.Find(id);
            if (r == null) return NotFound();
            return View(r);
        }

        // POST: /Reservations/Edit
        [HttpPost]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Edit(Reservation r)
        {
            if (!ModelState.IsValid)
                return View(r);

            _ctx.Reservations.Update(r);
            _ctx.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reservations/Delete/5
        // tylko Employee, Manager i Admin mogą usuwać
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Delete(int id)
        {
            var r = _ctx.Reservations.Find(id);
            if (r == null) return NotFound();
            return View(r);
        }

        // POST: /Reservations/Delete
        [HttpPost, ActionName("Delete")]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var r = _ctx.Reservations.Find(id);
            if (r != null)
            {
                _ctx.Reservations.Remove(r);
                _ctx.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
