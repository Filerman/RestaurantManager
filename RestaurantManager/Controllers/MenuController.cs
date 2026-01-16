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
using System.Collections.Generic;

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

        // --- GET: Index (Lista) ---
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            // isStaff decyduje CZY WIDAĆ pozycje ukryte (niedostępne). 
            // Pracownik nadal powinien je widzieć (np. żeby wiedzieć co jest w menu, nawet jak niedostępne).
            var isStaff = (role == "Admin" || role == "Manager" || role == "Employee");

            var query = _context.MenuItems.AsQueryable();

            if (!isStaff)
            {
                query = query.Where(m => m.IsAvailable);
            }

            return View(await query.ToListAsync());
        }

        // --- GET: Details (Szczegóły) ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            return View(menuItem);
        }

        // --- GET: Create ---
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesToViewBag();
            return View();
        }

        // --- POST: Create ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
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
                else
                {
                    item.ImagePath = null;
                }

                _context.MenuItems.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            await LoadCategoriesToViewBag();
            return View(item);
        }

        // --- GET: Edit ---
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            await LoadCategoriesToViewBag();
            return View(item);
        }

        // --- POST: Edit ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
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

            await LoadCategoriesToViewBag();
            return View(item);
        }

        // --- GET: Delete ---
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        // --- POST: Delete ---
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Manager", "Admin")] // USUNIĘTO "Employee"
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

        private async Task LoadCategoriesToViewBag()
        {
            var categories = await _context.MenuItems
                .Select(m => m.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
        }
    }
}