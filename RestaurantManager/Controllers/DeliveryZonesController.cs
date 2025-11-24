using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")]
    public class DeliveryZonesController : Controller
    {
        private readonly AppDbContext _context;

        public DeliveryZonesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DeliveryZones
        public async Task<IActionResult> Index()
        {
            // 1. AUTOMATYZACJA: Sprawdź, czy miasto restauracji jest na liście
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contactInfo != null && !string.IsNullOrEmpty(contactInfo.AddressCity))
            {
                var baseCityName = contactInfo.AddressCity.Trim();
                // Szukamy (ignorując wielkość liter)
                var exists = await _context.DeliveryZones
                    .AnyAsync(z => z.CityName.ToLower() == baseCityName.ToLower());

                if (!exists)
                {
                    // Jeśli nie ma, dodajemy automatycznie
                    _context.DeliveryZones.Add(new DeliveryZone
                    {
                        CityName = baseCityName,
                        DeliveryFee = 0 // Domyślnie 0, ale Manager może edytować
                    });
                    await _context.SaveChangesAsync();
                    TempData["InfoMessage"] = $"Dodano automatycznie miasto restauracji ({baseCityName}) do stref dostaw. Ustal dla niego opłatę.";
                }
            }

            return View(await _context.DeliveryZones.OrderBy(z => z.DeliveryFee).ToListAsync());
        }

        // GET: DeliveryZones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DeliveryZones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryZone deliveryZone)
        {
            if (ModelState.IsValid)
            {
                // Sprawdzenie duplikatów
                if (await _context.DeliveryZones.AnyAsync(z => z.CityName == deliveryZone.CityName))
                {
                    ModelState.AddModelError("CityName", "Ta miejscowość jest już na liście.");
                    return View(deliveryZone);
                }

                _context.Add(deliveryZone);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dodano nową strefę dostaw.";
                return RedirectToAction(nameof(Index));
            }
            return View(deliveryZone);
        }

        // GET: DeliveryZones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var deliveryZone = await _context.DeliveryZones.FindAsync(id);
            if (deliveryZone == null) return NotFound();
            return View(deliveryZone);
        }

        // POST: DeliveryZones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeliveryZone deliveryZone)
        {
            if (id != deliveryZone.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(deliveryZone);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Zaktualizowano strefę dostaw.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DeliveryZones.Any(e => e.Id == deliveryZone.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(deliveryZone);
        }

        // GET: DeliveryZones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var deliveryZone = await _context.DeliveryZones.FirstOrDefaultAsync(m => m.Id == id);
            if (deliveryZone == null) return NotFound();
            return View(deliveryZone);
        }

        // POST: DeliveryZones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deliveryZone = await _context.DeliveryZones.FindAsync(id);
            if (deliveryZone != null)
            {
                _context.DeliveryZones.Remove(deliveryZone);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usunięto strefę.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}