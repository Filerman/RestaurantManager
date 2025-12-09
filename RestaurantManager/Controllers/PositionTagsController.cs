using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")]
    public class PositionTagsController : Controller
    {
        private readonly AppDbContext _context;

        public PositionTagsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: PositionTags
        public async Task<IActionResult> Index()
        {
            return View(await _context.PositionTags.ToListAsync());
        }

        // GET: PositionTags/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var positionTag = await _context.PositionTags.FirstOrDefaultAsync(m => m.Id == id);
            if (positionTag == null) return NotFound();

            return View(positionTag);
        }

        // GET: PositionTags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PositionTags/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ZMIANA: Dodano HourlyRate do Bind
        public async Task<IActionResult> Create([Bind("Id,Name,HourlyRate")] PositionTag positionTag)
        {
            if (ModelState.IsValid)
            {
                _context.Add(positionTag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(positionTag);
        }

        // GET: PositionTags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var positionTag = await _context.PositionTags.FindAsync(id);
            if (positionTag == null) return NotFound();

            return View(positionTag);
        }

        // POST: PositionTags/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ZMIANA: Dodano HourlyRate do Bind
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,HourlyRate")] PositionTag positionTag)
        {
            if (id != positionTag.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(positionTag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PositionTags.Any(e => e.Id == positionTag.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(positionTag);
        }

        // GET: PositionTags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var positionTag = await _context.PositionTags.FirstOrDefaultAsync(m => m.Id == id);
            if (positionTag == null) return NotFound();

            return View(positionTag);
        }

        // POST: PositionTags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var positionTag = await _context.PositionTags.FindAsync(id);
            _context.PositionTags.Remove(positionTag);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}