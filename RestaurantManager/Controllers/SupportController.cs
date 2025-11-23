using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace RestaurantManager.Controllers
{
    // USUNIĘTO globalne RoleAuthorize, aby Goście (Klienci) mieli dostęp do wybranych akcji
    public class SupportController : Controller
    {
        private readonly AppDbContext _context;

        public SupportController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Support/MyTickets (Dla Pracownika i Klienta)
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

        // GET: /Support/Manage (Dla Managera - z filtrowaniem)
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> Manage(string filter = "all")
        {
            var query = _context.SupportTickets
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Logika filtrowania
            switch (filter)
            {
                case "employees":
                    // Pracownicy to ci, którzy NIE są Gośćmi (Guest)
                    query = query.Where(t => t.CreatedByUser.Role != "Guest");
                    break;
                case "clients":
                    // Klienci to Goście
                    query = query.Where(t => t.CreatedByUser.Role == "Guest");
                    break;
                case "all":
                default:
                    break;
            }

            var tickets = await query.OrderByDescending(t => t.DateCreated).ToListAsync();

            // Przekazujemy aktualny filtr do widoku, żeby ustawić selecta
            ViewBag.CurrentFilter = filter;

            return View(tickets);
        }

        // GET: /Support/Create (Dostępne dla każdego zalogowanego)
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST: /Support/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            ModelState.Remove(nameof(ticket.CreatedByUser));

            if (ModelState.IsValid)
            {
                ticket.CreatedByUserId = userId.Value;
                ticket.DateCreated = DateTime.Now;
                ticket.Status = TicketStatus.New;

                _context.Add(ticket);
                await _context.SaveChangesAsync();

                // Sprawdzamy rolę, żeby wyświetlić odpowiedni komunikat
                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Guest")
                    TempData["SuccessMessage"] = "Twoje pytanie zostało wysłane do managera.";
                else
                    TempData["SuccessMessage"] = "Zgłoszenie wysłane.";

                return RedirectToAction(nameof(MyTickets));
            }
            return View(ticket);
        }

        // GET: /Support/Details/5
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
        public async Task<IActionResult> ResolveTicket(int id, TicketStatus status, string managerResponse, bool addToFaq, string faqQuestion)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = status;
            ticket.ManagerResponse = managerResponse;

            // Logika dodawania do FAQ z edycją pytania
            if (addToFaq && !string.IsNullOrWhiteSpace(managerResponse))
            {
                // Używamy edytowanego pytania (faqQuestion) lub oryginału, jeśli pole puste
                string finalQuestion = !string.IsNullOrWhiteSpace(faqQuestion) ? faqQuestion : ticket.Subject;

                if (!string.IsNullOrWhiteSpace(finalQuestion))
                {
                    var newFaq = new FaqItem
                    {
                        Question = finalQuestion, // Zapisujemy poprawione pytanie
                        Answer = managerResponse, // Odpowiedź managera
                        IsPublic = true
                    };
                    _context.FaqItems.Add(newFaq);
                    TempData["InfoMessage"] = "Zgłoszenie rozwiązane i pomyślnie dodane do FAQ.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Zaktualizowano zgłoszenie.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
    }
}