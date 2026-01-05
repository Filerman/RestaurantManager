using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using RestaurantManager.Extensions;
using RestaurantManager.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantManager.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // --- SEKCJA POS (DLA PERSONELU) ---
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled)
                .OrderBy(o => o.ScheduledDate)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [RoleAuthorize("Admin", "Manager", "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- SEKCJA KLIENTA ---

        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["InfoMessage"] = "Zaloguj się, aby złożyć zamówienie.";
                return RedirectToAction("Login", "Auth");
            }

            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey);
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            var vm = new OrderCheckoutViewModel
            {
                TotalAmount = cart.Sum(x => x.TotalPrice),
                IsAsap = true, // Domyślnie zaznaczamy "Jak najszybciej"
                PaymentMethod = PaymentMethod.Cash // Domyślna metoda płatności
            };

            if (user != null)
            {
                vm.CustomerEmail = user.Email;
                vm.CustomerName = user.Username;

                // *** POPRAWKA: Automatyczne uzupełnianie telefonu z profilu ***
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    vm.CustomerPhone = user.PhoneNumber;
                }
            }

            // Pobieramy minimalny czas z bazy
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int deliveryMinutes = contactInfo?.EstimatedDeliveryTimeMinutes ?? 45;
            if (deliveryMinutes < 15) deliveryMinutes = 15;

            // Przekazujemy czas dostawy do widoku
            ViewBag.EstimatedTime = deliveryMinutes;

            // Pobieramy dzisiejsze godziny otwarcia dla informacji
            var today = DateTime.Now.DayOfWeek;
            var todayHours = await _context.OpeningHours.FirstOrDefaultAsync(h => h.DayOfWeek == today);
            ViewBag.TodayOpeningHours = todayHours;

            // Ustawiamy domyślną datę w kalendarzu na "Teraz + Czas"
            vm.ScheduledDate = DateTime.Now.AddMinutes(deliveryMinutes);

            PrepareCitiesList();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(OrderCheckoutViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey);
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Menu");

            // 1. Pobieramy ustawienia czasu
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int minMinutes = contactInfo?.EstimatedDeliveryTimeMinutes ?? 45;
            if (minMinutes < 15) minMinutes = 15;

            // 2. Obliczamy minimalną datę (Teraz + minuty)
            var minDeliveryTime = DateTime.Now.AddMinutes(minMinutes);

            // LOGIKA ASAP vs PLANOWANE
            if (model.IsAsap)
            {
                // Jeśli ASAP -> ignorujemy datę z formularza i ustawiamy najwcześniejszą możliwą
                model.ScheduledDate = minDeliveryTime;
                ModelState.Remove(nameof(model.ScheduledDate));
            }
            else
            {
                // Jeśli PLANOWANE -> sprawdzamy, czy klient nie wybrał za wcześnie
                if (model.ScheduledDate < minDeliveryTime.AddMinutes(-1))
                {
                    ModelState.AddModelError("ScheduledDate", $"Aktualny minimalny czas oczekiwania to {minMinutes} minut. Wybierz późniejszą godzinę.");
                }
            }

            // 3. WALIDACJA GODZIN OTWARCIA
            var dayOfWeek = model.ScheduledDate.DayOfWeek;
            var openingHour = await _context.OpeningHours.FirstOrDefaultAsync(oh => oh.DayOfWeek == dayOfWeek);

            if (openingHour != null)
            {
                if (openingHour.IsClosed)
                {
                    ModelState.AddModelError("ScheduledDate", "W wybranym dniu restauracja jest nieczynna.");
                }
                else
                {
                    var time = model.ScheduledDate.TimeOfDay;
                    // Sprawdzamy czy wybrana godzina mieści się w zakresie otwarcia
                    if (time < openingHour.OpenTime || time > openingHour.CloseTime)
                    {
                        ModelState.AddModelError("ScheduledDate", $"Restauracja jest nieczynna o tej godzinie. Zapraszamy w godzinach: {openingHour.OpenTime:hh\\:mm} - {openingHour.CloseTime:hh\\:mm}.");
                    }
                }
            }

            decimal deliveryFee = 0;

            // 4. Walidacja adresu i opłaty
            if (model.OrderType == OrderType.Delivery)
            {
                if (string.IsNullOrWhiteSpace(model.DeliveryStreet) ||
                    string.IsNullOrWhiteSpace(model.DeliveryCity) ||
                    string.IsNullOrWhiteSpace(model.DeliveryZipCode))
                {
                    ModelState.AddModelError("OrderType", "Dla dostawy wymagany jest pełny adres.");
                }
                else
                {
                    var zone = await _context.DeliveryZones
                        .FirstOrDefaultAsync(z => z.CityName.ToLower() == model.DeliveryCity.ToLower());

                    if (zone != null) deliveryFee = zone.DeliveryFee;
                    else ModelState.AddModelError("DeliveryCity", "Niestety, nie realizujemy dostaw do wybranej miejscowości.");
                }
            }

            if (model.OrderType != OrderType.Delivery)
            {
                ModelState.Remove(nameof(model.DeliveryStreet));
                ModelState.Remove(nameof(model.DeliveryCity));
                ModelState.Remove(nameof(model.DeliveryZipCode));
            }

            if (ModelState.IsValid)
            {
                string fullAddress = null;
                if (model.OrderType == OrderType.Delivery)
                {
                    fullAddress = $"{model.DeliveryStreet}, {model.DeliveryZipCode} {model.DeliveryCity}";
                }

                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    ScheduledDate = model.ScheduledDate,
                    Status = OrderStatus.Created,
                    Type = model.OrderType,
                    // *** NOWOŚĆ: Zapisujemy metodę płatności ***
                    PaymentMethod = model.PaymentMethod,
                    TotalAmount = cart.Sum(x => x.TotalPrice) + deliveryFee,
                    CustomerName = model.CustomerName,
                    CustomerPhone = model.CustomerPhone,
                    CustomerEmail = model.CustomerEmail,
                    DeliveryAddress = fullAddress,
                    Notes = model.Notes,
                    UserId = userId.Value.ToString()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    });
                }
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(CartSessionKey);

                return RedirectToAction(nameof(Confirmation), new { id = order.Id });
            }

            // Jeśli walidacja nie przeszła, przywracamy dane do widoku
            PrepareCitiesList();

            // Ponowne pobranie danych do ViewBag, aby wyświetliły się po błędzie
            var contactInfoForView = await _context.ContactInfos.FirstOrDefaultAsync();
            ViewBag.EstimatedTime = contactInfoForView?.EstimatedDeliveryTimeMinutes ?? 45;
            var todayForView = DateTime.Now.DayOfWeek;
            ViewBag.TodayOpeningHours = await _context.OpeningHours.FirstOrDefaultAsync(h => h.DayOfWeek == todayForView);

            model.TotalAmount = cart.Sum(x => x.TotalPrice);
            return View(model);
        }

        private void PrepareCitiesList()
        {
            var zones = _context.DeliveryZones.OrderBy(z => z.DeliveryFee).ToList();
            var cityList = new List<SelectListItem>();
            var feesDict = new Dictionary<string, decimal>();

            cityList.Add(new SelectListItem { Text = "-- Wybierz miasto --", Value = "" });

            foreach (var zone in zones)
            {
                string feeText = zone.DeliveryFee == 0 ? "Gratis" : $"+{zone.DeliveryFee:F2} zł";
                cityList.Add(new SelectListItem
                {
                    Text = $"{zone.CityName} ({feeText})",
                    Value = zone.CityName
                });
                feesDict[zone.CityName] = zone.DeliveryFee;
            }

            ViewBag.CityList = cityList;
            ViewBag.CityFeesJson = System.Text.Json.JsonSerializer.Serialize(feesDict);
        }

        // GET: /Orders/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (role != "Admin" && role != "Manager" && role != "Employee" && order.UserId != userId.ToString())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            return View(order);
        }

        // GET: /Orders/History
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var myOrders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Where(o => o.UserId == userId.Value.ToString())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }
    }
}