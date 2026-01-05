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
    public class SupportController : Controller
    {
        private readonly AppDbContext _context;

        public SupportController(AppDbContext context)
        {
            _context = context;
        }

        // 1. STREFA UŻYTKOWNIKA (PRACOWNIK / KLIENT)

        // GET: Support/Create
        // Dostępny dla każdego zalogowanego użytkownika
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");
            return View();
        }

        // POST: Support/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            // Usuwamy powiązanie z użytkownikiem z walidacji, bo ustawiamy je ręcznie
            ModelState.Remove(nameof(ticket.CreatedByUser));

            if (ModelState.IsValid)
            {
                ticket.CreatedByUserId = userId.Value;
                ticket.DateCreated = DateTime.Now;
                ticket.Status = TicketStatus.New;

                // Pole ManagerResponse jest nullable, więc domyślnie null

                _context.Add(ticket);
                await _context.SaveChangesAsync();

                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Guest")
                    TempData["SuccessMessage"] = "Twoje pytanie zostało wysłane. Oczekuj na odpowiedź.";
                else
                    TempData["SuccessMessage"] = "Zgłoszenie zostało wysłane do administratora.";

                return RedirectToAction(nameof(MyTickets));
            }
            return View(ticket);
        }

        // GET: Support/MyTickets
        // Wyświetla zgłoszenia ZALOGOWANEGO użytkownika
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

        // 2. STREFA MANAGERA (HELPDESK)

        // GET: Support/Manage
        // Panel zarządzania zgłoszeniami - tylko dla Managera/Admina
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
                    // Pokaż zgłoszenia od użytkowników, którzy NIE są Guest
                    query = query.Where(t => t.CreatedByUser.Role != "Guest");
                    break;
                case "clients":
                    // Pokaż zgłoszenia od Guest
                    query = query.Where(t => t.CreatedByUser.Role == "Guest");
                    break;
                case "open":
                    // Pokaż tylko nowe i w toku
                    query = query.Where(t => t.Status == TicketStatus.New || t.Status == TicketStatus.InProgress);
                    break;
                case "closed":
                    // Pokaż rozwiązane i odrzucone
                    query = query.Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Rejected);
                    break;
                case "all":
                default:
                    break;
            }

            var tickets = await query
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            ViewBag.CurrentFilter = filter;

            return View(tickets);
        }

        // GET: Support/Details/5
        // Widok szczegółów. Manager widzi formularz odpowiedzi, Użytkownik widzi status.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            bool isManager = (role == "Admin" || role == "Manager");

            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var ticket = await _context.SupportTickets
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            // Zabezpieczenie: Tylko autor LUB Manager może zobaczyć szczegóły
            if (!isManager && ticket.CreatedByUserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            return View(ticket);
        }

        // POST: Support/ResolveTicket
        // Akcja dla Managera do wysłania odpowiedzi i zmiany statusu
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Manager")]
        public async Task<IActionResult> ResolveTicket(int id, TicketStatus status, string managerResponse, bool addToFaq, string faqQuestion)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = status;
            ticket.ManagerResponse = managerResponse;

            // Logika dodawania do FAQ (opcjonalna)
            if (addToFaq && !string.IsNullOrWhiteSpace(managerResponse))
            {
                // Jeśli podano zmienione pytanie, użyj go, w przeciwnym razie temat zgłoszenia
                string questionText = !string.IsNullOrWhiteSpace(faqQuestion) ? faqQuestion : ticket.Subject;

                var newFaq = new FaqItem
                {
                    Question = questionText,
                    Answer = managerResponse,
                    IsPublic = true
                };
                _context.FaqItems.Add(newFaq);
                TempData["InfoMessage"] = "Zgłoszenie rozwiązane i dodane do FAQ.";
            }
            else
            {
                TempData["SuccessMessage"] = "Odpowiedź została wysłana.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
    }
}