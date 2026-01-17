using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class FaqControllerTests
    {
        [Fact]
        public async Task Index_ReturnsOnlyPublicFaqs_ForGuest()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            context.FaqItems.Add(new FaqItem { Question = "Publiczne?", Answer = "Tak", IsPublic = true });
            context.FaqItems.Add(new FaqItem { Question = "Wewnętrzne?", Answer = "Nie", IsPublic = false });
            await context.SaveChangesAsync();

            var controller = new FaqController(context);

            // Mock sesji - Rola "Guest" (Gość)
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession(); // Klasa z DbHelper.cs
            session.SetString("UserRole", "Guest");

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<FaqItem>>(viewResult.Model);

            Assert.Single(model); // Powinien widzieć tylko 1 element
            Assert.True(model.First().IsPublic);
        }

        [Fact]
        public async Task Index_ReturnsAllFaqs_ForAdmin()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Dodano wymagane pola Question i Answer, aby uniknąć DbUpdateException
            context.FaqItems.Add(new FaqItem { Question = "Q1", Answer = "A1", IsPublic = true });
            context.FaqItems.Add(new FaqItem { Question = "Q2", Answer = "A2", IsPublic = false });

            await context.SaveChangesAsync();

            var controller = new FaqController(context);

            // Mock sesji - Rola "Admin"
            var mockHttpContext = new Mock<HttpContext>();
            var session = new MockSession();
            session.SetString("UserRole", "Admin");

            mockHttpContext.Setup(s => s.Session).Returns(session);
            controller.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<FaqItem>>(viewResult.Model);

            Assert.Equal(2, model.Count()); // Admin widzi wszystko (2 elementy)
        }
    }
}