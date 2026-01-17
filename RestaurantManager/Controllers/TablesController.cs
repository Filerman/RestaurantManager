using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Filters;
using RestaurantManager.Models;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Manager", "Admin")]
    public class TablesController : Controller
    {
        private readonly AppDbContext _context;

        public TablesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Tables
        public async Task<IActionResult> Index()
        {
            var settings = await _context.ContactInfos.FirstOrDefaultAsync();
            ViewBag.DefaultOccupancyMinutes = settings?.DefaultTableOccupancyMinutes ?? 120;

            return View(await _context.Tables.ToListAsync());
        }

        // POST: /Tables/UpdateOccupancy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOccupancy(int minutes)
        {
            var settings = await _context.ContactInfos.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new ContactInfo
                {
                    AddressStreet = "-",
                    AddressCity = "-",
                    AddressZipCode = "-",
                    PhoneNumber = "-",
                    ContactEmail = "admin@example.com"
                };
                _context.ContactInfos.Add(settings);
            }

            settings.DefaultTableOccupancyMinutes = minutes;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Tables/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var table = await _context.Tables.FirstOrDefaultAsync(m => m.Id == id);
            if (table == null) return NotFound();

            return View(table);
        }

        // GET: /Tables/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Tables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Capacity,IsAvailable")] Table table)
        {
            if (ModelState.IsValid)
            {
                _context.Add(table);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // GET: /Tables/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            return View(table);
        }

        // POST: /Tables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Capacity,IsAvailable")] Table table)
        {
            if (id != table.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(table);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TableExists(table.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // GET: /Tables/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var table = await _context.Tables.FirstOrDefaultAsync(m => m.Id == id);
            if (table == null) return NotFound();

            return View(table);
        }

        // POST: /Tables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TableExists(int id)
        {
            return _context.Tables.Any(e => e.Id == id);
        }
    }
}