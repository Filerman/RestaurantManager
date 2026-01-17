using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class ContentControllerTests
    {
        [Fact]
        public async Task ManageHome_Post_UpdatesHomePageSettings()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Inicjalny stan
            context.HomePageSettings.Add(new HomePageSetting { Id = 1, HeroTitle = "Stary Tytuł" });
            await context.SaveChangesAsync();

            // Mock środowiska (wymagany przez konstruktor)
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            var controller = new ContentController(context, mockEnv.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var model = new HomeContentViewModel
            {
                Id = 1,
                HeroTitle = "Nowy Tytuł",
                HeroSubtitle = "Podtytuł"
            };

            // Act
            var result = await controller.ManageHome(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageHome", redirectResult.ActionName);

            var dbSettings = await context.HomePageSettings.FindAsync(1);
            Assert.Equal("Nowy Tytuł", dbSettings.HeroTitle);
            Assert.Equal("Podtytuł", dbSettings.HeroSubtitle);
        }
    }
}