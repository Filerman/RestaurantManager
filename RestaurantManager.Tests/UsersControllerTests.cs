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
    public class UsersControllerTests
    {
        private UsersController SetupController(Data.AppDbContext context, string userRole = "Admin")
        {
            var controller = new UsersController(context);

            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); // Klasa z DbHelper.cs
            session.SetString("UserRole", userRole); // Ustawiamy rolę zalogowanego użytkownika

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };
            controller.TempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task Edit_Post_PreventsManagerFromEditingAdmin()
        {
            // Test bezpieczeństwa: Czy Manager może edytować Admina? NIE POWINIEN.

            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Istniejący Admin w bazie
            var adminUser = new User { Id = 1, Username = "admin", Role = "Admin", Email = "admin@test.pl", Password = "123" };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Ustawiamy kontroler w kontekście zalogowanego MANAGERA
            var controller = SetupController(context, userRole: "Manager");

            // Dane formularza (próba zmiany)
            var editModel = new User { Id = 1, Username = "admin", Role = "Employee", Email = "admin@test.pl" };

            // Act
            var result = await controller.Edit(1, editModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Sprawdzamy czy pojawił się komunikat błędu
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.Contains("Manager nie może", controller.TempData["ErrorMessage"].ToString());

            // Weryfikacja: Rola w bazie NIE powinna się zmienić
            var dbUser = await context.Users.FindAsync(1);
            Assert.Equal("Admin", dbUser.Role);
        }

        [Fact]
        public async Task Delete_Post_ReturnsError_WhenDeletingLastAdmin()
        {
            // Test bezpieczeństwa: Czy można usunąć ostatniego Admina? NIE.

            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Tylko jeden Admin w bazie
            var lastAdmin = new User { Id = 1, Username = "admin", Role = "Admin", Email = "admin@test.pl", Password = "123" };
            context.Users.Add(lastAdmin);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userRole: "Admin");

            // Act
            var result = await controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            // Sprawdzamy błąd w TempData
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.Contains("ostatniego", controller.TempData["ErrorMessage"].ToString());

            // Weryfikacja: Użytkownik NADAL powinien istnieć w bazie
            Assert.Single(context.Users);
        }
    }
}