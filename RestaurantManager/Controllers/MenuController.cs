using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Models;
using System.Linq;
using RestaurantManager.Filters;

namespace RestaurantManager.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        // Widok menu jest publiczny
        public IActionResult Index()
        {
            var items = _context.MenuItems.ToList();
            return View(items);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult Create(MenuItem item)
        {
            if (ModelState.IsValid)
            {
                _context.MenuItems.Add(item);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(item);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult Edit(int id)
        {
            var item = _context.MenuItems.Find(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult Edit(MenuItem item)
        {
            if (ModelState.IsValid)
            {
                var existingItem = _context.MenuItems.Find(item.Id);
                if (existingItem == null)
                    return NotFound();

                existingItem.Name = item.Name;
                existingItem.Description = item.Description;
                existingItem.Price = item.Price;
                existingItem.Category = item.Category;
                existingItem.IsAvailable = item.IsAvailable; // To pole pozostaje

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(item);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult Delete(int id)
        {
            var item = _context.MenuItems.Find(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [RoleAuthorize("Employee", "Manager", "Admin")] // POPRAWKA
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _context.MenuItems.Find(id);
            if (item != null)
            {
                _context.MenuItems.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}