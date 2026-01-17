using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class PositionTagsControllerTests
    {
        [Fact]
        public async Task Create_Post_AddsNewTag()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            var controller = new PositionTagsController(context);
            var newTag = new PositionTag { Name = "Barman", HourlyRate = 25.50m };

            // Act
            var result = await controller.Create(newTag);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbTag = await context.PositionTags.FirstOrDefaultAsync();
            Assert.NotNull(dbTag);
            Assert.Equal("Barman", dbTag.Name);
            Assert.Equal(25.50m, dbTag.HourlyRate);
        }

        [Fact]
        public async Task Edit_Post_UpdatesHourlyRate()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.PositionTags.Add(new PositionTag { Id = 1, Name = "Kelner", HourlyRate = 15.00m });
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var controller = new PositionTagsController(context);
            var updatedTag = new PositionTag { Id = 1, Name = "Kelner", HourlyRate = 20.00m };

            // Act
            var result = await controller.Edit(1, updatedTag);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var dbTag = await context.PositionTags.FindAsync(1);
            Assert.Equal(20.00m, dbTag.HourlyRate);
        }
    }
}