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
    public class LossLogsControllerTests
    {
        private LossLogsController SetupController(Data.AppDbContext context, int? userId = null)
        {
            var controller = new LossLogsController(context);
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); 

            if (userId.HasValue) session.SetInt32("UserId", userId.Value);

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };
            controller.TempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task Report_Post_RedirectsToLogin_WhenUserNotLoggedIn()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = SetupController(context, userId: null); // Brak usera w sesji

            var lossLog = new LossLog();

            // Act
            var result = await controller.Report(lossLog, "custom");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Auth", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Report_Post_CalculatesEstimatedValue_ForMenuItem()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Dodajemy danie do menu (Cena = 50 zł)
            context.MenuItems.Add(new MenuItem { Id = 1, Name = "Stek", Price = 50, IsAvailable = true });
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 1); // Zalogowany user

            var lossLog = new LossLog
            {
                MenuItemId = 1,
                Quantity = 3,
                Description = "Spalone podczas pieczenia", // Wymagane pole
                LossType = "Zmarnowane" // POPRAWKA: String zamiast Enuma
            };

            // Act
            // Wybieramy opcję "menu", więc EstimatedValue powinno się wyliczyć z MenuItems
            var result = await controller.Report(lossLog, "menu");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Report", redirectResult.ActionName);

            var savedLog = Assert.Single(context.LossLogs);
            // 3 sztuki * 50 zł = 150 zł
            Assert.Equal(150m, savedLog.EstimatedValue);
        }
    }
}