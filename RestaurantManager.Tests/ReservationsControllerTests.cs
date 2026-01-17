using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class ReservationsControllerTests
    {
        [Fact]
        public async Task Create_Post_ReturnsError_WhenTableIsOccupied()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Konfiguracja czasu trwania wizyty (2h)
            context.ContactInfos.Add(new ContactInfo
            {
                DefaultTableOccupancyMinutes = 120,
                AddressCity = "City",
                AddressStreet = "Street",
                AddressZipCode = "00",
                PhoneNumber = "123",
                ContactEmail = "a@a.pl"
            });

            // Istniejąca rezerwacja: DZIŚ 18:00 - 20:00
            var today18 = DateTime.Today.AddHours(18);

            context.Reservations.Add(new Reservation
            {
                TableId = 1,
                DateTime = today18,
                Status = ReservationStatus.Confirmed,
                CustomerName = "Jan Kowalski",
                CustomerPhone = "123",
                CustomerEmail = "jan@test.pl"
            });
            await context.SaveChangesAsync();

            var controller = new ReservationsController(context);

            // Mock sesji (wymagane User ID)
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession();
            session.SetInt32("UserId", 1);

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

            // Act - Próba rezerwacji na 19:00 tego samego dnia (konflikt)
            var newReservation = new Reservation
            {
                TableId = 1,
                DateTime = today18.AddHours(1), // 19:00 (w trakcie innej rezerwacji)
                CustomerName = "Anna Nowak",
                CustomerPhone = "999",
                CustomerEmail = "anna@test.pl"
            };

            var result = controller.Create(newReservation);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("TableId")); // Błąd powinien dotyczyć stolika
        }
    }
}