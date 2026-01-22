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

            // ZMIANA: Używamy daty jutrzejszej, aby uniknąć błędu "rezerwacja w przeszłości"
            // niezależnie od godziny uruchomienia testu.
            var tomorrow18 = DateTime.Today.AddDays(1).AddHours(18);

            // Istniejąca rezerwacja: JUTRO 18:00 - 20:00
            context.Reservations.Add(new Reservation
            {
                TableId = 1,
                DateTime = tomorrow18,
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
                DateTime = tomorrow18.AddHours(1), // 19:00 (w trakcie innej rezerwacji)
                CustomerName = "Anna Nowak",
                CustomerPhone = "999",
                CustomerEmail = "anna@test.pl"
            };

            var result = controller.Create(newReservation);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(controller.ModelState.IsValid, "ModelState powinno być nieprawidłowe z powodu konfliktu.");
            Assert.True(controller.ModelState.ContainsKey("TableId"), "Powinien wystąpić błąd dotyczący zajętego stolika (TableId).");
        }
    }
}