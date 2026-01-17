using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System;
using Xunit;

namespace RestaurantManager.Tests
{
    public class AvailabilityControllerTests
    {
        [Fact]
        public void Add_Post_ReturnsError_WhenEndTimeIsBeforeStartTime()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = new AvailabilityController(context);

            // Konfiguracja Sesji i TempData
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); // Nasza klasa

            // SYMULACJA ZALOGOWANIA: Wpisujemy ID=1 wprost do sesji
            session.SetInt32("UserId", 1);

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };
            controller.TempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());

            var invalidAvailability = new Availability
            {
                UserId = 1, // Zgadza się z sesją
                Date = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(10, 0, 0) // Błąd logiczny
            };

            // Act
            var result = controller.Add(invalidAvailability);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result); // Teraz powinno zwrócić widok z błędem
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("NewAvailability.EndTime"));
        }
    }
}