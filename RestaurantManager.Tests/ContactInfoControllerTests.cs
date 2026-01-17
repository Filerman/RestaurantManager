using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore; // Ważne dla FirstOrDefaultAsync
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class ContactInfoControllerTests
    {
        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Dodano wymagane pola, aby uniknąć DbUpdateException
            context.ContactInfos.Add(new ContactInfo
            {
                RestaurantName = "Stara Nazwa",
                EstimatedDeliveryTimeMinutes = 30,
                AddressCity = "Old City",
                AddressStreet = "Old Street",
                AddressZipCode = "00-000",
                PhoneNumber = "000000000",
                ContactEmail = "old@test.com"
            });
            await context.SaveChangesAsync();

            var controller = new ContactInfoController(context);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var model = new ContactInfoEditViewModel
            {
                Contact = new ContactInfo
                {
                    RestaurantName = "Nowa Nazwa",
                    EstimatedDeliveryTimeMinutes = 45,
                    AddressCity = "New City",
                    AddressStreet = "New Street",
                    AddressZipCode = "11-111",
                    PhoneNumber = "123123123",
                    ContactEmail = "new@test.com"
                }
            };

            // Act
            var result = await controller.Edit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Sprawdzamy czy dane w bazie się zmieniły
            var dbContact = await context.ContactInfos.FirstOrDefaultAsync();
            Assert.Equal("Nowa Nazwa", dbContact.RestaurantName);
            Assert.Equal(45, dbContact.EstimatedDeliveryTimeMinutes);
            Assert.Equal("New City", dbContact.AddressCity);
        }
    }
}