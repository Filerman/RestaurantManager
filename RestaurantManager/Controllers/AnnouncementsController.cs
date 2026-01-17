using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace RestaurantManager.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AnnouncementsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Announcements 
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var activeNews = await _context.Announcements
                .Where(a => a.ValidFrom <= now && (a.ValidUntil == null || a.ValidUntil >= now))
                .OrderByDescending(a => a.ValidFrom) 
                .ToListAsync();

            return View(activeNews);
        }

        // GET: Announcements/Manage (Panel Managera)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage()
        {
            var allNews = await _context.Announcements
                .OrderByDescending(a => a.ValidFrom)
                .ToListAsync();
            return View(allNews);
        }

        // GET: Announcements/Create
        [RoleAuthorize("Admin", "Manager")]
        public IActionResult Create()
        {
            var vm = new AnnouncementViewModel
            {
                ValidFrom = DateTime.Now
            };
            return View(vm);
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Create(AnnouncementViewModel model)
        {
            // Walidacja dat
            if (model.ValidUntil.HasValue && model.ValidUntil < model.ValidFrom)
            {
                ModelState.AddModelError("ValidUntil", "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }

            if (ModelState.IsValid)
            {
                string imagePath = null;

                if (model.Image != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "announcements");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fileStream);
                    }
                    imagePath = "/images/announcements/" + uniqueFileName;
                }

                var announcement = new Announcement
                {
                    Title = model.Title,
                    Content = model.Content,
                    ValidFrom = model.ValidFrom,
                    ValidUntil = model.ValidUntil,
                    DateCreated = DateTime.Now,
                    ImagePath = imagePath
                };

                _context.Add(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dodano nowe ogłoszenie.";
                return RedirectToAction(nameof(Manage));
            }
            return View(model);
        }

        // GET: Announcements/Edit/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            var vm = new AnnouncementViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                ValidFrom = announcement.ValidFrom, 
                ValidUntil = announcement.ValidUntil,
                ExistingImagePath = announcement.ImagePath
            };

            return View(vm);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int id, AnnouncementViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (model.ValidUntil.HasValue && model.ValidUntil < model.ValidFrom)
            {
                ModelState.AddModelError("ValidUntil", "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var announcement = await _context.Announcements.FindAsync(id);
                    if (announcement == null) return NotFound();

                    announcement.Title = model.Title;
                    announcement.Content = model.Content;
                    announcement.ValidFrom = model.ValidFrom;
                    announcement.ValidUntil = model.ValidUntil;

                    if (model.Image != null)
                    {
                        if (!string.IsNullOrEmpty(announcement.ImagePath))
                        {
                            string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, announcement.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "announcements");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Image.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.Image.CopyToAsync(fileStream);
                        }
                        announcement.ImagePath = "/images/announcements/" + uniqueFileName;
                    }

                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ogłoszenie zaktualizowane.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Announcements.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }
            return View(model);
        }

        // GET: Announcements/Delete/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var announcement = await _context.Announcements.FirstOrDefaultAsync(m => m.Id == id);
            if (announcement == null) return NotFound();
            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                if (!string.IsNullOrEmpty(announcement.ImagePath))
                {
                    string path = Path.Combine(_webHostEnvironment.WebRootPath, announcement.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ogłoszenie usunięte.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}