using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class TablesControllerTests
    {
        [Fact]
        public async Task UpdateOccupancy_Post_UpdatesSettings()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Inicjalizacja ustawień
            context.ContactInfos.Add(new ContactInfo
            {
                DefaultTableOccupancyMinutes = 60,
                // Wymagane pola, by uniknąć DbUpdateException
                AddressCity = "-",
                AddressStreet = "-",
                AddressZipCode = "-",
                PhoneNumber = "-",
                ContactEmail = "test@test.pl"
            });
            await context.SaveChangesAsync();

            var controller = new TablesController(context);

            // Act
            var result = await controller.UpdateOccupancy(90);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var settings = await context.ContactInfos.FirstOrDefaultAsync();
            Assert.Equal(90, settings.DefaultTableOccupancyMinutes);
        }

        [Fact]
        public async Task Create_Post_AddsNewTable()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = new TablesController(context);
            var newTable = new Table { Name = "Stolik przy oknie", Capacity = 4, IsAvailable = true };

            // Act
            var result = await controller.Create(newTable);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbTable = await context.Tables.FirstOrDefaultAsync();
            Assert.NotNull(dbTable);
            Assert.Equal("Stolik przy oknie", dbTable.Name);
        }
    }
}