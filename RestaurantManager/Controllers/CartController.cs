using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Data;
using RestaurantManager.Extensions;
using RestaurantManager.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantManager.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            return View(cart);
        }

        // POST: /Cart/Add/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int id, int quantity = 1, string returnUrl = null)
        {
            if (quantity < 1) quantity = 1;

            var menuItem = _context.MenuItems.Find(id);
            if (menuItem == null || !menuItem.IsAvailable)
            {
                return NotFound();
            }

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(i => i.MenuItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    Price = menuItem.Price,
                    Quantity = quantity 
                });
            }

            SaveCartToSession(cart);

            TempData["SuccessMessage"] = quantity > 1
                ? $"Dodano {quantity}x {menuItem.Name} do koszyka."
                : $"Dodano {menuItem.Name} do koszyka.";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            string referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index", "Menu");
        }

        // POST: /Cart/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.MenuItemId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
                TempData["SuccessMessage"] = "Usunięto pozycję z koszyka.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.MenuItemId == id);
            if (item != null)
            {
                if (quantity > 0)
                    item.Quantity = quantity;
                else
                    cart.Remove(item); // Jeśli 0, usuwamy

                SaveCartToSession(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // Metody pomocnicze
        private List<CartItemViewModel> GetCartFromSession()
        {
            return HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? new List<CartItemViewModel>();
        }

        private void SaveCartToSession(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }
    }
}