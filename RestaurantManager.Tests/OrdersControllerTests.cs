using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class OrdersControllerPosTests
    {
        [Fact]
        public async Task UpdateStatus_Post_ChangesOrderStatus()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Wymagane ContactInfo (bo kontroler może go używać w innych akcjach, tu dla bezpieczeństwa)
            context.ContactInfos.Add(new ContactInfo
            {
                EstimatedDeliveryTimeMinutes = 30,
                AddressCity = "City",
                AddressStreet = "Street",
                AddressZipCode = "00",
                PhoneNumber = "123",
                ContactEmail = "a@a.pl"
            });

            var order = new Order
            {
                Id = 1,
                Status = OrderStatus.Created,
                CustomerName = "Test",
                CustomerPhone = "123",
                CustomerEmail = "a@a.pl",
                DeliveryAddress = "Adres"
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new OrdersController(context);

            // Act
            var result = await controller.UpdateStatus(1, OrderStatus.Ready);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var dbOrder = await context.Orders.FindAsync(1);
            Assert.Equal(OrderStatus.Ready, dbOrder.Status);
        }

        [Fact]
        public async Task CloseTable_Post_SetsStatusToCompleted()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            context.ContactInfos.Add(new ContactInfo
            {
                EstimatedDeliveryTimeMinutes = 30,
                AddressCity = "City",
                AddressStreet = "Street",
                AddressZipCode = "00",
                PhoneNumber = "123",
                ContactEmail = "a@a.pl"
            });

            var order = new Order
            {
                Id = 1,
                Status = OrderStatus.Ready,
                CustomerName = "Stolik 1",
                CustomerPhone = "000",
                CustomerEmail = "a@a.pl",
                DeliveryAddress = "-"
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new OrdersController(context);

            // Act
            var result = await controller.CloseTable(1, PaymentMethod.Card);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Receipt", redirectResult.ActionName); // Powinno przekierować do rachunku

            var dbOrder = await context.Orders.FindAsync(1);
            Assert.Equal(OrderStatus.Completed, dbOrder.Status);
            Assert.Equal(PaymentMethod.Card, dbOrder.PaymentMethod);
        }
    }
}