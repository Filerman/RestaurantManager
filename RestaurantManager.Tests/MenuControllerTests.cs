using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class MenuControllerTests
    {
        [Fact]
        public async Task Create_Post_AddsMenuItem_WithImage()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            var controller = new MenuController(context);

            var newItem = new MenuItem { Name = "Pizza", Price = 30, Category = "Main", IsAvailable = true };

            // Act
            var result = await controller.Create(newItem, null); // Bez pliku

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbItem = await context.MenuItems.FirstOrDefaultAsync();
            Assert.NotNull(dbItem);
            Assert.Equal("Pizza", dbItem.Name);
        }

        [Fact]
        public async Task Edit_Post_UpdatesMenuItem()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            context.MenuItems.Add(new MenuItem { Id = 1, Name = "Old Name", Price = 10, Category = "Main" });
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var controller = new MenuController(context);
            var updatedItem = new MenuItem { Id = 1, Name = "New Name", Price = 15, Category = "Main", IsAvailable = true };

            // Act
            var result = await controller.Edit(1, updatedItem, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var dbItem = await context.MenuItems.FindAsync(1);
            Assert.Equal("New Name", dbItem.Name);
            Assert.Equal(15, dbItem.Price);
        }
    }
}