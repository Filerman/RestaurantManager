using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using RestaurantManager.Filters;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Manager", "Admin")]
    public class ContentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContentController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> ManageHome()
        {
            // Pobieramy ustawienia (powinien być tylko jeden rekord, jeśli nie ma - tworzymy domyślny)
            var settings = await _context.HomePageSettings
                .Include(x => x.CarouselImages)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new HomePageSetting();
                _context.HomePageSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var vm = new HomeContentViewModel
            {
                Id = settings.Id,
                HeroTitle = settings.HeroTitle,
                HeroSubtitle = settings.HeroSubtitle,
                ExistingImages = settings.CarouselImages.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageHome(HomeContentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = await _context.HomePageSettings.FindAsync(model.Id);
                if (settings == null) return NotFound();

                settings.HeroTitle = model.HeroTitle;
                settings.HeroSubtitle = model.HeroSubtitle;

                // Obsługa zdjęć
                if (model.NewImages != null && model.NewImages.Count > 0)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "carousel");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    foreach (var file in model.NewImages)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadFolder, uniqueName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            var img = new CarouselImage
                            {
                                ImagePath = "/images/carousel/" + uniqueName,
                                HomePageSettingId = settings.Id
                            };
                            _context.CarouselImages.Add(img);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zaktualizowano treść strony głównej.";
                return RedirectToAction(nameof(ManageHome));
            }

            // Przeładuj zdjęcia w przypadku błędu walidacji
            model.ExistingImages = await _context.CarouselImages.Where(x => x.HomePageSettingId == model.Id).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _context.CarouselImages.FindAsync(id);
            if (img != null)
            {
                // Opcjonalnie: usuń plik fizycznie z dysku
                var path = Path.Combine(_webHostEnvironment.WebRootPath, img.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                _context.CarouselImages.Remove(img);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zdjęcie usunięte.";
            }
            return RedirectToAction(nameof(ManageHome));
        }
    }
}