using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace RestaurantManager.Tests
{
    public class CartControllerTests
    {
        private CartController SetupController(Data.AppDbContext context)
        {
            var controller = new CartController(context);

            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); // Nasza klasa z DbHelper.cs

            // 1. Mockowanie Sesji
            mockHttpContext.Setup(s => s.Session).Returns(session);

            // 2. Mockowanie Request i Headers (NAPRAWA BŁĘDU NullReferenceException)
            var mockRequest = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            mockRequest.Setup(r => r.Headers).Returns(headers);
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };
            controller.TempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public void Add_Post_AddsNewItemToCart_WhenItemIsNotInCart()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.MenuItems.Add(new MenuItem { Id = 1, Name = "Pizza", Price = 30, IsAvailable = true });
            context.SaveChanges();

            var controller = SetupController(context);

            // Act
            var result = controller.Add(id: 1, quantity: 2);

            // Assert
            // Kontroler przekierowuje do Index/Menu jeśli nie ma referera
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            // Sprawdzamy zawartość sesji
            var cartJson = controller.HttpContext.Session.GetString("Cart");
            Assert.NotNull(cartJson);
            var cart = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);

            Assert.Single(cart);
            Assert.Equal(1, cart[0].MenuItemId);
            Assert.Equal(2, cart[0].Quantity);
        }

        [Fact]
        public void Add_Post_IncrementsQuantity_WhenItemAlreadyInCart()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.MenuItems.Add(new MenuItem { Id = 1, Name = "Burger", Price = 20, IsAvailable = true });
            context.SaveChanges();

            var controller = SetupController(context);

            // Wstępne dodanie produktu do koszyka
            var initialCart = new List<CartItemViewModel>
            {
                new CartItemViewModel { MenuItemId = 1, Quantity = 1, Price = 20 }
            };
            controller.HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(initialCart));

            // Act
            controller.Add(id: 1, quantity: 2);

            // Assert
            var cartJson = controller.HttpContext.Session.GetString("Cart");
            var cart = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);

            Assert.Single(cart);
            Assert.Equal(3, cart[0].Quantity); // 1 + 2 = 3
        }

        [Fact]
        public void Remove_Post_RemovesItemFromCart()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = SetupController(context);

            var initialCart = new List<CartItemViewModel>
            {
                new CartItemViewModel { MenuItemId = 1, Quantity = 1 },
                new CartItemViewModel { MenuItemId = 2, Quantity = 1 }
            };
            controller.HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(initialCart));

            // Act
            controller.Remove(id: 1);

            // Assert
            var cartJson = controller.HttpContext.Session.GetString("Cart");
            var cart = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);

            Assert.Single(cart);
            Assert.Equal(2, cart[0].MenuItemId);
        }
    }
}