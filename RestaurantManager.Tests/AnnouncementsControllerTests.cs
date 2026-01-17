using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class AnnouncementsControllerTests
    {
        // Pomocnicza metoda do konfiguracji kontrolera
        private AnnouncementsController SetupController(Data.AppDbContext context)
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            // Mockujemy ścieżkę do plików, bo kontroler używa IWebHostEnvironment
            mockEnvironment.Setup(m => m.WebRootPath).Returns("wwwroot");

            return new AnnouncementsController(context, mockEnvironment.Object);
        }

        // TEST 1: Logika biznesowa - Filtrowanie ogłoszeń (CMS)
        [Fact]
        public async Task Index_ReturnsOnlyActiveAnnouncements()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var now = DateTime.Now;

            // Dodajemy 3 ogłoszenia:
            // 1. Aktywne (zaczęło się wczoraj, kończy jutro)
            context.Announcements.Add(new Announcement
            {
                Title = "Active",
                Content = "Test",
                ValidFrom = now.AddDays(-1),
                ValidUntil = now.AddDays(1)
            });
            // 2. Przyszłe (zaczyna się jutro) - NIE POWINNO BYĆ WIDOCZNE
            context.Announcements.Add(new Announcement
            {
                Title = "Future",
                Content = "Test",
                ValidFrom = now.AddDays(1),
                ValidUntil = now.AddDays(2)
            });
            // 3. Archiwalne (skończyło się wczoraj) - NIE POWINNO BYĆ WIDOCZNE
            context.Announcements.Add(new Announcement
            {
                Title = "Expired",
                Content = "Test",
                ValidFrom = now.AddDays(-2),
                ValidUntil = now.AddDays(-1)
            });

            await context.SaveChangesAsync();

            var controller = SetupController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Announcement>>(viewResult.Model);

            // Oczekujemy tylko 1 ogłoszenia (tego aktywnego)
            Assert.Single(model);
            Assert.Equal("Active", model.First().Title);
        }

        // TEST 2: Walidacja - Blokada błędnych dat (Data Do < Data Od)
        [Fact]
        public async Task Create_Post_ReturnsError_WhenValidUntilIsBeforeValidFrom()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = SetupController(context);

            var invalidModel = new AnnouncementViewModel
            {
                Title = "Błędne ogłoszenie",
                Content = "Treść",
                ValidFrom = DateTime.Now.AddDays(5),
                ValidUntil = DateTime.Now.AddDays(1) // Błąd: Koniec przed początkiem
            };

            // Act
            var result = await controller.Create(invalidModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // Sprawdzenie czy ModelState jest niepoprawny (powinien zawierać błąd)
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("ValidUntil"));
        }

        // TEST 3: Integracyjny (z bazą in-memory) - Poprawne dodanie
        [Fact]
        public async Task Create_Post_AddsAnnouncementToDatabase_WhenModelIsValid()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = SetupController(context);

            // Mock TempData (wymagane, bo kontroler ustawia SuccessMessage)
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
            );

            var validModel = new AnnouncementViewModel
            {
                Title = "Nowe menu",
                Content = "Zapraszamy!",
                ValidFrom = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(7)
            };

            // Act
            var result = await controller.Create(validModel);

            // Assert
            // Powinno przekierować do akcji Manage
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Manage", redirectResult.ActionName);

            // Sprawdzamy czy w bazie faktycznie jest rekord
            var savedAnnouncement = Assert.Single(context.Announcements);
            Assert.Equal("Nowe menu", savedAnnouncement.Title);
        }
    }

    public static class Extension
    {
        public static T Of<T>() where T : class => new Mock<T>().Object;
    }
}