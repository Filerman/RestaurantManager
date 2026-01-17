using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class DeliveryZonesControllerTests
    {
        [Fact]
        public async Task Create_Post_ReturnsError_WhenCityAlreadyExists()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.DeliveryZones.Add(new DeliveryZone { CityName = "Warszawa", DeliveryFee = 10 });
            await context.SaveChangesAsync();

            var controller = new DeliveryZonesController(context);
            var duplicateZone = new DeliveryZone { CityName = "Warszawa", DeliveryFee = 20 };

            // Act
            var result = await controller.Create(duplicateZone);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("CityName"));
        }

        [Fact]
        public async Task UpdateDeliveryTime_Post_UpdatesContactInfo()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Inicjalizacja ContactInfo (wymagane pola)
            context.ContactInfos.Add(new ContactInfo
            {
                EstimatedDeliveryTimeMinutes = 30,
                AddressCity = "City",
                AddressStreet = "Street",
                AddressZipCode = "00",
                PhoneNumber = "123",
                ContactEmail = "a@a.pl"
            });
            await context.SaveChangesAsync();

            var controller = new DeliveryZonesController(context);
            controller.TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = await controller.UpdateDeliveryTime(60);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var updatedInfo = await context.ContactInfos.FirstOrDefaultAsync();
            Assert.Equal(60, updatedInfo.EstimatedDeliveryTimeMinutes);
        }
    }
}