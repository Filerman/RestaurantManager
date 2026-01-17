using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class GalleryControllerTests
    {
        [Fact]
        public async Task Upload_Post_ReturnsError_WhenFileExtensionIsInvalid()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            var controller = new GalleryController(context, mockEnv.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Symulacja pliku tekstowego (niedozwolonego)
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.Length).Returns(100);

            // Act
            var result = await controller.Upload(mockFile.Object, "Test caption");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Manage", redirectResult.ActionName);
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.Contains("Niedozwolony format", controller.TempData["ErrorMessage"].ToString());
        }

        [Fact]
        public async Task ToggleCarousel_Post_UpdatesDatabase()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.GalleryImages.Add(new GalleryImage { Id = 1, IsInCarousel = false, FilePath = "/img.jpg" });
            await context.SaveChangesAsync();

            var controller = new GalleryController(context, Mock.Of<IWebHostEnvironment>());
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = await controller.ToggleCarousel(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var img = await context.GalleryImages.FindAsync(1);
            Assert.True(img.IsInCarousel); // Powinno zmienić się na true
        }
    }
}