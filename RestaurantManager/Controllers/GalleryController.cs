using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters; // Potrzebne dla RoleAuthorize
using System.IO; // Potrzebne dla Path
using System.Threading.Tasks; // Potrzebne dla Task
using Microsoft.AspNetCore.Hosting; // Potrzebne dla IWebHostEnvironment
using System.Linq; // Potrzebne dla ToListAsync
using Microsoft.AspNetCore.Http; // Potrzebne dla IFormFile
using System; // Potrzebne dla Guid

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

        // [GET] /Gallery/Index - Publiczna galeria
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var images = await _context.GalleryImages.OrderByDescending(i => i.Id).ToListAsync();
            return View(images);
        }

        // [GET] /Gallery/Manage - Zarządzanie galerią (Admin/Manager)
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Manage()
        {
            var images = await _context.GalleryImages.OrderByDescending(i => i.Id).ToListAsync();
            return View(images);
        }

        // [POST] /Gallery/Upload - Dodawanie nowego zdjęcia (Admin/Manager)
        [HttpPost]
        [RoleAuthorize("Manager", "Admin")]
        [ValidateAntiForgeryToken] // Dobre dla bezpieczeństwa
        public async Task<IActionResult> Upload(IFormFile imageFile, string caption)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Nie wybrano pliku.";
                return RedirectToAction(nameof(Manage));
            }

            // Walidacja typu pliku (prosta)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Niedozwolony format pliku. Dozwolone są: .jpg, .jpeg, .png, .gif";
                return RedirectToAction(nameof(Manage));
            }

            // Ścieżka do zapisu w wwwroot
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "gallery");
            // Upewnij się, że folder istnieje
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                // Zapisz plik na serwerze
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Zapisz ścieżkę względną w bazie danych
                var newImage = new GalleryImage
                {
                    // Ważne: Zapisujemy ścieżkę względną, której może użyć HTML
                    FilePath = "/images/gallery/" + uniqueFileName,
                    Caption = caption
                };

                _context.GalleryImages.Add(newImage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zdjęcie zostało pomyślnie dodane.";
            }
            catch (Exception ex)
            {
                // TODO: Logowanie błędu (ex)
                TempData["ErrorMessage"] = "Wystąpił błąd podczas przesyłania pliku: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        // [POST] /Gallery/Delete - Usuwanie zdjęcia (Admin/Manager)
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
                // 1. Usuń plik fizyczny z serwera
                // Konwertuj ścieżkę względną (np. /images/gallery/...) na pełną ścieżkę systemową
                string relativePath = image.FilePath;
                // Usuń wiodący '/' jeśli istnieje, aby Path.Combine działał poprawnie
                if (relativePath.StartsWith("/"))
                {
                    relativePath = relativePath.Substring(1);
                }
                // Zamień separatory / na natywne dla systemu \ (jeśli trzeba)
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
                else
                {
                    // Plik nie istnieje, ale i tak usuwamy wpis z bazy
                    TempData["InfoMessage"] = "Nie znaleziono pliku fizycznego, ale usunięto wpis z bazy.";
                }

                // 2. Usuń wpis z bazy danych
                _context.GalleryImages.Remove(image);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zdjęcie zostało usunięte.";
            }
            catch (Exception ex)
            {
                // TODO: Logowanie błędu (ex)
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania zdjęcia: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}