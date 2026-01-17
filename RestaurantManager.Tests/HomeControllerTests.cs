using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class HomeControllerTests
    {
        [Fact]
        public async Task Index_ReturnsDefaultSettings_WhenDatabaseIsEmpty()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Upewniamy się, że baza jest pusta
            // (DbHelper tworzy nową bazę dla każdego testu, więc jest pusta)

            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockLogger.Object, context);

            // Mock sesji
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Session).Returns(new MockSession());
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomePageSetting>(viewResult.Model);

            // Kontroler powinien stworzyć domyślny obiekt, jeśli w bazie nic nie ma
            Assert.Contains("Najlepsze smaki", model.HeroTitle);
        }
    }
}