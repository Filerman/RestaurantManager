using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Dla HttpContext.Session
using System; // Dla DateTime

namespace RestaurantManager.Controllers
{
    public class LossLogsController : Controller
    {
        private readonly AppDbContext _context;

        public LossLogsController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /LossLogs/Index - Rejestr strat (Admin/Manager)
        [HttpGet]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Index()
        {
            var lossLogs = await _context.LossLogs
                .Include(l => l.ReportedByUser)
                .OrderByDescending(l => l.DateReported)
                .ToListAsync();

            return View(lossLogs);
        }

        // [GET] /LossLogs/Report - Formularz zgłaszania straty (Pracownicy)
        [HttpGet]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public IActionResult Report()
        {
            // Przekazujemy nowy model do widoku, aby bindowanie asp-for działało poprawnie
            var model = new LossLog
            {
                DateReported = DateTime.Now
            };
            return View(model);
        }

        // [POST] /LossLogs/Report - Zapisanie straty
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Employee", "Manager", "Admin")]
        public async Task<IActionResult> Report([Bind("Description,LossType,EstimatedValue")] LossLog lossLog)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // *** POPRAWKA ***
            // Ustawiamy pola, które nie pochodzą z formularza, PRZED sprawdzeniem ModelState
            lossLog.ReportedByUserId = userId.Value;
            lossLog.DateReported = DateTime.Now;

            // Błąd "The Zgłoszony przez field is required" jest mylący.
            // Walidator próbuje sprawdzić właściwość nawigacyjną 'ReportedByUser' (która jest null).
            // Mówimy walidatorowi, aby zignorował ten błąd, ponieważ my i tak ręcznie ustawiamy ID.
            ModelState.Remove(nameof(lossLog.ReportedByUser));
            // *** KONIEC POPRAWKI ***

            if (ModelState.IsValid)
            {
                _context.LossLogs.Add(lossLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Strata została pomyślnie zarejestrowana.";
                // Wracamy do formularza, aby można było zgłosić kolejną stratę
                return RedirectToAction(nameof(Report));
            }

            // Jeśli model nadal nie jest poprawny (np. brak opisu), wróć do widoku z błędami
            // Błąd "Zgłoszony przez" już się nie pojawi, ale błędy "Opis" lub "Wartość" tak.
            return View(lossLog);
        }


        // [POST] /LossLogs/Delete - Usuwanie wpisu (Admin/Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Manager", "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var lossLog = await _context.LossLogs.FindAsync(id);
            if (lossLog == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono wpisu.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.LossLogs.Remove(lossLog);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Wpis straty został pomyślnie usunięty.";
            }
            catch (Exception ex)
            {
                // TODO: Logowanie błędu (ex)
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania wpisu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}