using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace RestaurantManager.Controllers
{
    public class GalleryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GalleryController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // [GET] /Gallery/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var images = await _context.GalleryImages.OrderByDescending(i => i.Id).ToListAsync();
            return View(images);
        }

        // [GET] /Gallery/Manage
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Manage()
        {
            var images = await _context.GalleryImages.OrderByDescending(i => i.Id).ToListAsync();
            return View(images);
        }

        // [POST] /Gallery/ToggleCarousel - NOWA METODA
        [HttpPost]
        [RoleAuthorize("Manager", "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCarousel(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image != null)
            {
                image.IsInCarousel = !image.IsInCarousel; // Przełącz flagę
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zaktualizowano status karuzeli.";
            }
            return RedirectToAction(nameof(Manage));
        }

        // [POST] /Gallery/Upload
        [HttpPost]
        [RoleAuthorize("Manager", "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile imageFile, string caption)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Nie wybrano pliku.";
                return RedirectToAction(nameof(Manage));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Niedozwolony format pliku.";
                return RedirectToAction(nameof(Manage));
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "gallery");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                var newImage = new GalleryImage
                {
                    FilePath = "/images/gallery/" + uniqueFileName,
                    Caption = caption,
                    IsInCarousel = false // Domyślnie nie w karuzeli
                };

                _context.GalleryImages.Add(newImage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zdjęcie zostało dodane.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Błąd: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        // [POST] /Gallery/Delete
        [HttpPost]
        [RoleAuthorize("Manager", "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono zdjęcia.";
                return RedirectToAction(nameof(Manage));
            }

            try
            {
                string relativePath = image.FilePath;
                if (relativePath.StartsWith("/")) relativePath = relativePath.Substring(1);
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.GalleryImages.Remove(image);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zdjęcie usunięte.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Błąd: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}