using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.Filters;
using System.Linq;
using System.Threading.Tasks;
using RestaurantManager.ViewModels; // Dodaj using dla ViewModels
using System; // Dla DayOfWeek

namespace RestaurantManager.Controllers
{
    [RoleAuthorize("Admin", "Manager")]
    public class ScheduleTemplatesController : Controller
    {
        private readonly AppDbContext _context;

        public ScheduleTemplatesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ScheduleTemplates
        public async Task<IActionResult> Index()
        {
            var templates = await _context.ScheduleTemplates
                                        .Include(t => t.ShiftSlots) // Dołączamy sloty, aby pokazać ich liczbę
                                        .OrderBy(t => t.Name)
                                        .ToListAsync();
            return View(templates);
        }

        // GET: ScheduleTemplates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var template = await _context.ScheduleTemplates
                .Include(t => t.ShiftSlots)
                    .ThenInclude(sl => sl.RequiredPositionTag) // Dołączamy nazwy tagów
                .FirstOrDefaultAsync(m => m.Id == id);

            if (template == null) return NotFound();

            // Sortowanie slotów dla lepszej czytelności
            template.ShiftSlots = template.ShiftSlots
                                        .OrderBy(sl => sl.DayOfWeek)
                                        .ThenBy(sl => sl.StartTime)
                                        .ToList();

            return View(template);
        }

        // GET: ScheduleTemplates/Create
        public IActionResult Create()
        {
            // Widok Create będzie zawierał tylko pole na nazwę
            return View();
        }

        // POST: ScheduleTemplates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] ScheduleTemplate scheduleTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(scheduleTemplate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Utworzono szablon '{scheduleTemplate.Name}'. Teraz dodaj wymagane zmiany.";
                // Przekieruj do edycji slotów nowo utworzonego szablonu
                return RedirectToAction(nameof(Edit), new { id = scheduleTemplate.Id });
            }
            // Jeśli nazwa jest nieprawidłowa, wróć do formularza
            return View(scheduleTemplate);
        }

        // GET: ScheduleTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var template = await _context.ScheduleTemplates
                .Include(t => t.ShiftSlots) // Załaduj istniejące sloty
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null) return NotFound();

            // Mapowanie na ViewModel
            var viewModel = new ScheduleTemplateEditViewModel
            {
                Id = template.Id,
                Name = template.Name,
                Slots = template.ShiftSlots.Select(sl => new TemplateShiftSlotViewModel
                {
                    Id = sl.Id,
                    DayOfWeek = sl.DayOfWeek,
                    StartTime = sl.StartTime,
                    EndTime = sl.EndTime,
                    PositionTagId = sl.PositionTagId,
                    RequiredEmployeeCount = sl.RequiredEmployeeCount
                })
                .OrderBy(svm => svm.DayOfWeek).ThenBy(svm => svm.StartTime) // Sortowanie
                .ToList()
            };

            // Przygotuj dane dla dropdownów
            await PopulateDropdownsAsync();

            return View(viewModel);
        }

        // POST: ScheduleTemplates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScheduleTemplateEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            // Sprawdź czy sama nazwa jest poprawna (ignorujemy błędy walidacji slotów na tym etapie)
            if (string.IsNullOrWhiteSpace(viewModel.Name) || viewModel.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Nazwa szablonu jest wymagana i nie może przekraczać 100 znaków.");
                // Musimy ponownie załadować dropdowny
                await PopulateDropdownsAsync();
                return View(viewModel);
            }


            // Ręczna walidacja każdego slota, bo ModelState może być skomplikowany z listą
            bool slotsAreValid = true;
            if (viewModel.Slots != null)
            {
                foreach (var slotVm in viewModel.Slots)
                {
                    // Sprawdź tylko te, które nie są do usunięcia
                    if (!slotVm.IsMarkedForDeletion)
                    {
                        if (slotVm.EndTime <= slotVm.StartTime)
                        {
                            // Można dodać bardziej szczegółowy błąd, ale na razie ogólny
                            ModelState.AddModelError("", $"Błąd w slocie dla {slotVm.DayOfWeek}: Godzina zakończenia musi być po godzinie rozpoczęcia.");
                            slotsAreValid = false;
                        }
                        if (slotVm.PositionTagId <= 0)
                        {
                            ModelState.AddModelError("", $"Błąd w slocie dla {slotVm.DayOfWeek}: Należy wybrać Tag stanowiska.");
                            slotsAreValid = false;
                        }
                        if (slotVm.RequiredEmployeeCount <= 0)
                        {
                            ModelState.AddModelError("", $"Błąd w slocie dla {slotVm.DayOfWeek}: Wymagana liczba pracowników musi być większa od 0.");
                            slotsAreValid = false;
                        }
                        // Można dodać więcej walidacji
                    }
                }
            }


            if (!ModelState.IsValid || !slotsAreValid)
            {
                // Jeśli są błędy, wróć do formularza
                await PopulateDropdownsAsync();
                return View(viewModel);
            }

            // --- Logika Zapisywania ---
            try
            {
                var templateToUpdate = await _context.ScheduleTemplates
                    .Include(t => t.ShiftSlots) // Załaduj istniejące sloty do porównania
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (templateToUpdate == null) return NotFound();

                // 1. Zaktualizuj nazwę szablonu
                templateToUpdate.Name = viewModel.Name;

                // 2. Przetwórz sloty
                if (viewModel.Slots != null)
                {
                    foreach (var slotVm in viewModel.Slots)
                    {
                        if (slotVm.Id > 0) // Istniejący slot
                        {
                            var existingSlot = templateToUpdate.ShiftSlots.FirstOrDefault(sl => sl.Id == slotVm.Id);
                            if (existingSlot != null)
                            {
                                if (slotVm.IsMarkedForDeletion)
                                {
                                    _context.TemplateShiftSlots.Remove(existingSlot); // Usuń
                                }
                                else // Zaktualizuj
                                {
                                    existingSlot.DayOfWeek = slotVm.DayOfWeek;
                                    existingSlot.StartTime = slotVm.StartTime;
                                    existingSlot.EndTime = slotVm.EndTime;
                                    existingSlot.PositionTagId = slotVm.PositionTagId;
                                    existingSlot.RequiredEmployeeCount = slotVm.RequiredEmployeeCount;
                                    _context.Entry(existingSlot).State = EntityState.Modified;
                                }
                            }
                        }
                        else if (!slotVm.IsMarkedForDeletion) // Nowy slot (Id = 0) i nie jest do usunięcia
                        {
                            var newSlot = new TemplateShiftSlot
                            {
                                ScheduleTemplateId = templateToUpdate.Id,
                                DayOfWeek = slotVm.DayOfWeek,
                                StartTime = slotVm.StartTime,
                                EndTime = slotVm.EndTime,
                                PositionTagId = slotVm.PositionTagId,
                                RequiredEmployeeCount = slotVm.RequiredEmployeeCount
                            };
                            _context.TemplateShiftSlots.Add(newSlot);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Szablon grafiku został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Wystąpił konflikt współbieżności. Spróbuj ponownie.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił nieoczekiwany błąd: {ex.Message}");
            }

            // Jeśli doszło do błędu zapisu, wróć do formularza
            await PopulateDropdownsAsync();
            return View(viewModel);
        }


        // GET: ScheduleTemplates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var template = await _context.ScheduleTemplates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (template == null) return NotFound();

            return View(template);
        }

        // POST: ScheduleTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var template = await _context.ScheduleTemplates.FindAsync(id);
            if (template != null)
            {
                // Dzięki Cascade Delete, sloty zostaną usunięte automatycznie
                _context.ScheduleTemplates.Remove(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Szablon grafiku został usunięty.";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- Metody Pomocnicze ---

        // Metoda do ładowania danych dla dropdownów (Tagi, Dni Tygodnia)
        private async Task PopulateDropdownsAsync()
        {
            ViewBag.PositionTagId = new SelectList(await _context.PositionTags.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");

            // Przygotowanie listy dni tygodnia
            var daysOfWeek = Enum.GetValues(typeof(DayOfWeek))
                                 .Cast<DayOfWeek>()
                                 .Select(d => new SelectListItem
                                 {
                                     Value = ((int)d).ToString(), // Wartość to liczba (0=Niedziela, 1=Poniedziałek...)
                                     Text = d.ToString() // Tekst to nazwa dnia
                                 })
                                 .ToList();
            ViewBag.DayOfWeek = new SelectList(daysOfWeek, "Value", "Text");
        }
    }
}