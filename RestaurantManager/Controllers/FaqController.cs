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

        // GET: /Faq (Dla wszystkich)
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            bool isStaff = (role == "Admin" || role == "Manager" || role == "Employee");

            // Jeśli pracownik/admin - pokaż wszystko. Jeśli klient - tylko publiczne.
            var faqs = isStaff
                ? await _context.FaqItems.ToListAsync()
                : await _context.FaqItems.Where(f => f.IsPublic).ToListAsync();

            return View(faqs);
        }

        // GET: /Faq/Manage (Tylko Manager/Admin)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage()
        {
            return View(await _context.FaqItems.ToListAsync());
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
                TempData["SuccessMessage"] = "Dodano pytanie do FAQ.";
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
                _context.Update(faqItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zaktualizowano FAQ.";
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
                TempData["SuccessMessage"] = "Usunięto wpis FAQ.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}