using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels; // Dodaj ten using
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace RestaurantManager.Controllers
{
    public class ContactInfoController : Controller
    {
        private readonly AppDbContext _context;

        public ContactInfoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /ContactInfo
        public async Task<IActionResult> Index()
        {
            var contact = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contact == null) contact = new ContactInfo();

            ViewBag.OpeningHours = await _context.OpeningHours
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToListAsync();

            return View(contact);
        }

        // GET: /ContactInfo/Edit
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit()
        {
            // 1. Pobierz dane kontaktowe
            var contact = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contact == null)
            {
                // *** POPRAWKA: Tworzymy tylko obiekt w pamięci, NIE zapisujemy go do bazy z nullami ***
                contact = new ContactInfo();
            }

            // 2. Pobierz godziny otwarcia
            var hours = await _context.OpeningHours
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToListAsync();

            // Jeśli z jakiegoś powodu brak godzin w bazie (np. błąd seedera), utwórz domyślne w pamięci
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

        // POST: /ContactInfo/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(ContactInfoEditViewModel model)
        {
            // Walidacja dotyczy głównie modelu Contact, godziny walidujemy ręcznie lub są zawsze poprawne
            // Ignorujemy błędy walidacji dla OpeningHours, bo MVC może zgłupieć przy listach
            ModelState.Remove(nameof(model.OpeningHours));

            if (ModelState.IsValid)
            {
                // 1. Zapis Danych Kontaktowych
                var dbContact = await _context.ContactInfos.FirstOrDefaultAsync();
                if (dbContact == null)
                {
                    _context.ContactInfos.Add(model.Contact);
                }
                else
                {
                    dbContact.RestaurantName = model.Contact.RestaurantName;
                    dbContact.AddressStreet = model.Contact.AddressStreet;
                    dbContact.AddressCity = model.Contact.AddressCity;
                    dbContact.AddressZipCode = model.Contact.AddressZipCode;
                    dbContact.PhoneNumber = model.Contact.PhoneNumber;
                    dbContact.ContactEmail = model.Contact.ContactEmail;
                    dbContact.GoogleMapsLink = model.Contact.GoogleMapsLink;
                    _context.Update(dbContact);
                }

                // 2. Zapis Godzin Otwarcia
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
                        else if (hour.Id == 0) // Nowy rekord (gdyby baza była pusta)
                        {
                            _context.OpeningHours.Add(hour);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zaktualizowano dane firmy i godziny otwarcia.";
                return RedirectToAction(nameof(Index));
            }

            // Jeśli błąd walidacji, zwróć widok
            return View(model);
        }
    }
}