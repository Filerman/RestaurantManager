using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using System.Linq;
using System.Threading.Tasks;
using RestaurantManager.Filters;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic; // Potrzebne do list

namespace RestaurantManager.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly string _imageFolder = "images/menu";

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        // Widok menu jest publiczny
        public async Task<IActionResult> Index()
        {
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Employee");
            var query = _context.MenuItems.AsQueryable();

            if (!isStaff)
            {
                query = query.Where(m => m.IsAvailable);
            }

            return View(await query.ToListAsync());
        }

        // GET: Menu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            return View(menuItem);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Create()
        {
            // Pobieramy istniejące kategorie do podpowiedzi
            var categories = await _context.MenuItems
                .Select(m => m.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Create(MenuItem item, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _imageFolder);
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    var fileName = $"{Guid.NewGuid()}_{Path.GetExtension(imageFile.FileName)}";
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    item.ImagePath = "/" + Path.Combine(_imageFolder, fileName).Replace("\\", "/");
                }

                _context.MenuItems.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            // W razie błędu walidacji musimy ponownie załadować kategorie
            var categories = await _context.MenuItems
               .Select(m => m.Category)
               .Where(c => c != null && c != "")
               .Distinct()
               .OrderBy(c => c)
               .ToListAsync();
            ViewBag.Categories = categories;

            return View(item);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return NotFound();

            // Pobieramy istniejące kategorie do podpowiedzi
            var categories = await _context.MenuItems
                .Select(m => m.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Edit(int id, MenuItem item, IFormFile? imageFile)
        {
            if (id != item.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingItem = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _imageFolder);
                        if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                        var fileName = $"{Guid.NewGuid()}_{Path.GetExtension(imageFile.FileName)}";
                        var filePath = Path.Combine(uploads, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        if (existingItem != null && !string.IsNullOrEmpty(existingItem.ImagePath))
                        {
                            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingItem.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        item.ImagePath = "/" + Path.Combine(_imageFolder, fileName).Replace("\\", "/");
                    }
                    else
                    {
                        item.ImagePath = existingItem?.ImagePath;
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MenuItems.Any(e => e.Id == item.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Index");
            }

            // W razie błędu walidacji ponownie ładujemy kategorie
            var categories = await _context.MenuItems
               .Select(m => m.Category)
               .Where(c => c != null && c != "")
               .Distinct()
               .OrderBy(c => c)
               .ToListAsync();
            ViewBag.Categories = categories;

            return View(item);
        }

        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                _context.MenuItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}