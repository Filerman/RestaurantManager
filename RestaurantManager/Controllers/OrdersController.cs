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
        public async Task<IActionResult> Index(string activeTab = "online")
        {
            ViewBag.ActiveTab = activeTab;

            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled)
                .OrderBy(o => o.ScheduledDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [RoleAuthorize("Admin", "Manager", "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string returnTab = "online")
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { activeTab = returnTab });
        }

        // --- MAPA STOLIKÓW ---
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> Tables()
        {
            var tables = await _context.Tables.ToListAsync();

            var busyTableIds = await _context.Orders
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled && o.Type == OrderType.DineIn && o.TableId != null)
                .Select(o => o.TableId.Value)
                .ToListAsync();

            ViewBag.BusyTableIds = busyTableIds;
            return View(tables);
        }

        // --- ZARZĄDZANIE KONKRETNYM STOLIKIEM ---
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> ManageTable(int id, string activeCategory = null)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            var activeOrder = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.TableId == id && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled);

            var menuItems = await _context.MenuItems.Where(m => m.IsAvailable).ToListAsync();
            var categories = menuItems.Select(m => m.Category).Distinct().ToList();

            ViewBag.ActiveCategory = activeCategory ?? (categories.FirstOrDefault() ?? "");

            var vm = new ManageTableViewModel
            {
                Table = table,
                CurrentOrder = activeOrder,
                MenuItems = menuItems,
                Categories = categories
            };

            return View(vm);
        }

        // --- DODAWANIE DO STOLIKA ---
        [HttpPost]
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> AddToTable(int tableId, int menuItemId, string returnCategory)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null) return NotFound();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.TableId == tableId && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled);

            if (order == null)
            {
                order = new Order
                {
                    TableId = tableId,
                    OrderDate = DateTime.Now,
                    ScheduledDate = DateTime.Now,
                    Status = OrderStatus.Created,
                    Type = OrderType.DineIn,
                    CustomerName = $"Stolik #{tableId}",
                    TotalAmount = 0
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = menuItemId,
                Quantity = 1,
                UnitPrice = menuItem.Price,
                IsServed = false
            };

            order.TotalAmount += menuItem.Price;
            _context.OrderItems.Add(orderItem);

            if (order.Status == OrderStatus.Ready) order.Status = OrderStatus.InKitchen;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageTable), new { id = tableId, activeCategory = returnCategory });
        }

        // --- ODZNACZANIE WYDANYCH DAŃ ---
        [HttpPost]
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> ToggleItemServed(int orderItemId, int tableId)
        {
            var item = await _context.OrderItems.FindAsync(orderItemId);
            if (item != null)
            {
                item.IsServed = !item.IsServed;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageTable), new { id = tableId });
        }

        // --- ZAMYKANIE STOLIKA ---
        [HttpPost]
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> CloseTable(int orderId, PaymentMethod paymentMethod)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.Completed;
                order.PaymentMethod = paymentMethod;
                order.ScheduledDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Receipt), new { id = orderId });
        }

        // --- WIDOK RACHUNKU ---
        public async Task<IActionResult> Receipt(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            bool isStaff = role == "Admin" || role == "Manager" || role == "Employee";
            if (!isStaff && (userId == null || order.UserId != userId.ToString()))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // --- POPRAWKA: Składamy adres ręcznie ---
            var contactInfo = await _context.ContactInfos.AsNoTracking().FirstOrDefaultAsync();

            ViewBag.RestaurantName = contactInfo?.RestaurantName ?? "Restauracja";

            // Tutaj łączymy stringi adresu
            if (contactInfo != null)
            {
                ViewBag.Address = $"{contactInfo.AddressStreet}, {contactInfo.AddressZipCode} {contactInfo.AddressCity}";
            }
            else
            {
                ViewBag.Address = "Brak adresu";
            }

            ViewBag.Phone = contactInfo?.PhoneNumber;

            return View(order);
        }

        // --- ARCHIWUM ZAMÓWIEŃ ---
        [RoleAuthorize("Admin", "Manager", "Employee")]
        public async Task<IActionResult> Archive()
        {
            var archivedOrders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Canceled)
                .OrderByDescending(o => o.ScheduledDate)
                .ToListAsync();

            return View(archivedOrders);
        }

        // --- SEKCJA KLIENTA (E-COMMERCE) ---

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
                IsAsap = true,
                PaymentMethod = PaymentMethod.Cash,
                OrderType = OrderType.Delivery
            };

            if (user != null)
            {
                vm.CustomerEmail = user.Email;
                vm.CustomerName = user.Username;
                if (!string.IsNullOrEmpty(user.PhoneNumber)) vm.CustomerPhone = user.PhoneNumber;
            }

            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int deliveryMinutes = contactInfo?.EstimatedDeliveryTimeMinutes ?? 45;
            if (deliveryMinutes < 15) deliveryMinutes = 15;

            ViewBag.EstimatedTime = deliveryMinutes;
            var today = DateTime.Now.DayOfWeek;
            ViewBag.TodayOpeningHours = await _context.OpeningHours.FirstOrDefaultAsync(h => h.DayOfWeek == today);

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

            if (model.OrderType == OrderType.DineIn)
            {
                ModelState.AddModelError("OrderType", "Zamówienia 'Na miejscu' są obsługiwane tylko przez obsługę w lokalu.");
            }

            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey);
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Menu");

            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            int minMinutes = contactInfo?.EstimatedDeliveryTimeMinutes ?? 45;
            if (minMinutes < 15) minMinutes = 15;
            var minDeliveryTime = DateTime.Now.AddMinutes(minMinutes);

            if (model.IsAsap)
            {
                model.ScheduledDate = minDeliveryTime;
                ModelState.Remove(nameof(model.ScheduledDate));
            }
            else
            {
                if (model.ScheduledDate < minDeliveryTime.AddMinutes(-1))
                {
                    ModelState.AddModelError("ScheduledDate", $"Aktualny minimalny czas oczekiwania to {minMinutes} minut. Wybierz późniejszą godzinę.");
                }
            }

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
                    if (time < openingHour.OpenTime || time > openingHour.CloseTime)
                    {
                        ModelState.AddModelError("ScheduledDate", $"Restauracja jest nieczynna o tej godzinie. Zapraszamy w godzinach: {openingHour.OpenTime:hh\\:mm} - {openingHour.CloseTime:hh\\:mm}.");
                    }
                }
            }

            decimal deliveryFee = 0;
            if (model.OrderType == OrderType.Delivery)
            {
                if (string.IsNullOrWhiteSpace(model.DeliveryCity))
                    ModelState.AddModelError("DeliveryCity", "Wymagane miasto.");
                else
                {
                    var zone = await _context.DeliveryZones.FirstOrDefaultAsync(z => z.CityName.ToLower() == model.DeliveryCity.ToLower());
                    if (zone != null) deliveryFee = zone.DeliveryFee;
                    else ModelState.AddModelError("DeliveryCity", "Brak dostaw do tej miejscowości.");
                }
            }
            else
            {
                ModelState.Remove(nameof(model.DeliveryStreet));
                ModelState.Remove(nameof(model.DeliveryCity));
                ModelState.Remove(nameof(model.DeliveryZipCode));
            }

            if (ModelState.IsValid)
            {
                string fullAddress = model.OrderType == OrderType.Delivery ? $"{model.DeliveryStreet}, {model.DeliveryZipCode} {model.DeliveryCity}" : null;

                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    ScheduledDate = model.ScheduledDate,
                    Status = OrderStatus.Created,
                    Type = model.OrderType,
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

            PrepareCitiesList();
            return View(model);
        }

        private void PrepareCitiesList()
        {
            var zones = _context.DeliveryZones.OrderBy(z => z.DeliveryFee).ToList();
            var cityList = new List<SelectListItem> { new SelectListItem { Text = "-- Wybierz miasto --", Value = "" } };
            var feesDict = new Dictionary<string, decimal>();

            foreach (var zone in zones)
            {
                string feeText = zone.DeliveryFee == 0 ? "Gratis" : $"+{zone.DeliveryFee:F2} zł";
                cityList.Add(new SelectListItem { Text = $"{zone.CityName} ({feeText})", Value = zone.CityName });
                feesDict[zone.CityName] = zone.DeliveryFee;
            }

            ViewBag.CityList = cityList;
            ViewBag.CityFeesJson = System.Text.Json.JsonSerializer.Serialize(feesDict);
        }

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