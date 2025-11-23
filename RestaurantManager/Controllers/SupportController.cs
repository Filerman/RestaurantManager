using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager", "Employee")]
    public class SupportController : Controller
    {
        private readonly AppDbContext _context;

        public SupportController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Support/MyTickets (Dla Pracownika - widzi tylko swoje)
        public async Task<IActionResult> MyTickets()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var tickets = await _context.SupportTickets
                .Where(t => t.CreatedByUserId == userId.Value)
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            return View(tickets);
        }

        // GET: /Support/Manage (Dla Managera - widzi wszystkie)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.CreatedByUser)
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();
            return View(tickets);
        }

        // GET: /Support/Create
        public IActionResult Create() => View();

        // POST: /Support/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            // Ignorujemy pola ustawiane automatycznie
            ModelState.Remove(nameof(ticket.CreatedByUser));

            if (ModelState.IsValid)
            {
                ticket.CreatedByUserId = userId.Value;
                ticket.DateCreated = DateTime.Now;
                ticket.Status = TicketStatus.New;

                _context.Add(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zgłoszenie wysłane.";
                return RedirectToAction(nameof(MyTickets));
            }
            return View(ticket);
        }

        // GET: /Support/Details/5 (Do podglądu i zmiany statusu przez Managera)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var ticket = await _context.SupportTickets
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        // POST: /Support/ResolveTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> ResolveTicket(int id, TicketStatus status, string managerResponse)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = status;
            ticket.ManagerResponse = managerResponse; // Zapisujemy odpowiedź

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Zaktualizowano zgłoszenie.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
    }
}