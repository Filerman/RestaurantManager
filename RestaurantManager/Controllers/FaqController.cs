using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RestaurantManager.Controllers
{
    public class FaqController : Controller
    {
        private readonly AppDbContext _context;

        public FaqController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Faq/Index
        public async Task<IActionResult> Index(string filter = "all")
        {
            var role = HttpContext.Session.GetString("UserRole");
            bool isStaff = (role == "Admin" || role == "Manager" || role == "Employee");

            var query = _context.FaqItems.AsQueryable();

            if (!isStaff)
            {
                query = query.Where(f => f.IsPublic);
            }
            else
            {
                switch (filter)
                {
                    case "internal":
                        query = query.Where(f => !f.IsPublic);
                        break;
                    case "public":
                        query = query.Where(f => f.IsPublic);
                        break;
                }
            }

            var faqs = await query.ToListAsync();

            ViewBag.IsStaff = isStaff;
            ViewBag.CurrentFilter = filter;

            return View(faqs);
        }

        // GET: /Faq/Manage
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage()
        {
            var allFaqs = await _context.FaqItems.OrderByDescending(f => f.Id).ToListAsync();
            return View(allFaqs);
        }

        // GET: /Faq/Create
        [RoleAuthorize("Admin", "Manager")]
        public IActionResult Create() => View();

        // POST: /Faq/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Create(FaqItem faqItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faqItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dodano nowe pytanie do FAQ.";
                return RedirectToAction(nameof(Manage));
            }
            return View(faqItem);
        }

        // GET: /Faq/Edit/5
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var faqItem = await _context.FaqItems.FindAsync(id);
            if (faqItem == null) return NotFound();
            return View(faqItem);
        }

        // POST: /Faq/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Edit(int id, FaqItem faqItem)
        {
            if (id != faqItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faqItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Zaktualizowano wpis FAQ.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.FaqItems.Any(e => e.Id == faqItem.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }
            return View(faqItem);
        }

        // POST: /Faq/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var faqItem = await _context.FaqItems.FindAsync(id);
            if (faqItem != null)
            {
                _context.FaqItems.Remove(faqItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usunięto pytanie z FAQ.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}