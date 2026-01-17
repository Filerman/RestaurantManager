using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class AuthControllerTests
    {
        private AuthController SetupController(Data.AppDbContext context)
        {
            var controller = new AuthController(context);

            // Ustawiamy naszą "fałszywą" sesję
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); // Używamy klasy z DbHelper.cs
            mockHttpContext.Setup(s => s.Session).Returns(session);

            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

            // Ustawiamy TempData (niezbędne dla komunikatów błędów)
            var tempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task Register_Post_ReturnsViewWithError_WhenUsernameExists()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.Users.Add(new User
            {
                Username = "existingUser",
                Email = "test@test.pl",
                Password = "123",
                Role = "Guest"
            });
            await context.SaveChangesAsync();

            var controller = SetupController(context);

            // Act
            var result = await controller.Register("existingUser", "new@test.pl", "password", "123456789", "1234");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.TempData.ContainsKey("Error"));
            Assert.Contains("zajęty", controller.TempData["Error"].ToString());
        }

        [Fact]
        public void Login_Post_RedirectsToHome_WhenCredentialsAreCorrect()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.Users.Add(new User
            {
                Username = "validUser",
                Email = "valid@test.pl",
                Password = "correctPassword",
                Role = "Guest"
            });
            context.SaveChanges();

            var controller = SetupController(context);

            // Act
            var result = controller.Login("validUser", "correctPassword");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // Dodatkowe sprawdzenie: czy sesja została ustawiona?
            Assert.NotNull(controller.HttpContext.Session.GetString("Username"));
        }
    }
}