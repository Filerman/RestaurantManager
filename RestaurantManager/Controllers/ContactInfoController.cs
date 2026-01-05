using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace RestaurantManager.Controllers
{
    public class ContactInfoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly string _logoFolder = "images/logos";

        public ContactInfoController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var contact = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contact == null) contact = new ContactInfo();

            ViewBag.OpeningHours = await _context.OpeningHours
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToListAsync();

            return View(contact);
        }

        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit()
        {
            var contact = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contact == null) contact = new ContactInfo();

            var hours = await _context.OpeningHours
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToListAsync();

            if (!hours.Any())
            {
                var days = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
                foreach (var day in days)
                {
                    hours.Add(new OpeningHour
                    {
                        DayOfWeek = day,
                        OpenTime = new TimeSpan(12, 0, 0),
                        CloseTime = new TimeSpan(22, 0, 0)
                    });
                }
                hours = hours.OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek).ToList();
            }

            var vm = new ContactInfoEditViewModel
            {
                Contact = contact,
                OpeningHours = hours
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(ContactInfoEditViewModel model)
        {
            ModelState.Remove(nameof(model.OpeningHours));
            // Usuwamy walidację pola Contact.LogoPath, bo może być nullem
            ModelState.Remove("Contact.LogoPath");

            if (ModelState.IsValid)
            {
                var dbContact = await _context.ContactInfos.FirstOrDefaultAsync();
                bool isNew = dbContact == null;

                if (isNew)
                {
                    dbContact = new ContactInfo();
                    _context.ContactInfos.Add(dbContact);
                }

                // 1. Aktualizacja prostych pól
                dbContact.RestaurantName = model.Contact.RestaurantName;
                dbContact.AddressStreet = model.Contact.AddressStreet;
                dbContact.AddressCity = model.Contact.AddressCity;
                dbContact.AddressZipCode = model.Contact.AddressZipCode;
                dbContact.PhoneNumber = model.Contact.PhoneNumber;
                dbContact.ContactEmail = model.Contact.ContactEmail;
                dbContact.GoogleMapsLink = model.Contact.GoogleMapsLink;
                dbContact.EstimatedDeliveryTimeMinutes = model.Contact.EstimatedDeliveryTimeMinutes;

                // *** NOWOŚĆ: Zapis flagi wyboru ***
                dbContact.ShowLogoInHeader = model.Contact.ShowLogoInHeader;

                // 2. Obsługa zapisu Logo z Base64 (z Croppera)
                if (!string.IsNullOrEmpty(model.CroppedLogoBase64))
                {
                    // Przygotuj folder
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _logoFolder);
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    // Konwersja Base64 na plik
                    // Format stringa: "data:image/png;base64,iVBORw0KGgo..."
                    var base64Data = model.CroppedLogoBase64.Split(',')[1];
                    var bytes = Convert.FromBase64String(base64Data);

                    // Unikalna nazwa
                    var fileName = $"logo_{Guid.NewGuid().ToString().Substring(0, 8)}.png";
                    var filePath = Path.Combine(uploads, fileName);

                    // Usuń stare logo
                    if (!string.IsNullOrEmpty(dbContact.LogoPath))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", dbContact.LogoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) try { System.IO.File.Delete(oldPath); } catch { }
                    }

                    // Zapisz nowy
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    dbContact.LogoPath = "/" + Path.Combine(_logoFolder, fileName).Replace("\\", "/");

                    // Jeśli wgrano logo, automatycznie przełącz na widok logo (opcjonalne UX)
                    dbContact.ShowLogoInHeader = true;
                }

                // 3. Zapis Godzin
                if (model.OpeningHours != null)
                {
                    foreach (var hour in model.OpeningHours)
                    {
                        var dbHour = await _context.OpeningHours.FindAsync(hour.Id);
                        if (dbHour != null)
                        {
                            dbHour.OpenTime = hour.OpenTime;
                            dbHour.CloseTime = hour.CloseTime;
                            dbHour.IsClosed = hour.IsClosed;
                            _context.Update(dbHour);
                        }
                        else if (hour.Id == 0)
                        {
                            _context.OpeningHours.Add(hour);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zaktualizowano konfigurację.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // *** NOWOŚĆ: Akcja usuwania logo ***
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> DeleteLogo()
        {
            var contact = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contact != null && !string.IsNullOrEmpty(contact.LogoPath))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contact.LogoPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) try { System.IO.File.Delete(oldPath); } catch { }

                contact.LogoPath = null;
                contact.ShowLogoInHeader = false; // Wracamy do nazwy tekstowej
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Logo zostało usunięte.";
            }
            return RedirectToAction(nameof(Edit));
        }
    }
}