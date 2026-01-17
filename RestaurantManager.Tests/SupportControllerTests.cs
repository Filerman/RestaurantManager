using Microsoft.AspNetCore.Http;
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
    public class SupportControllerTests
    {
        [Fact]
        public async Task ResolveTicket_Post_AddsToFaq_WhenRequested()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Dodajemy istniejące zgłoszenie
            var ticket = new SupportTicket
            {
                Id = 1,
                Subject = "Problem z logowaniem",
                Description = "Nie mogę wejść",
                CreatedByUserId = 1,
                Status = TicketStatus.New
            };
            context.SupportTickets.Add(ticket);
            await context.SaveChangesAsync();

            var controller = new SupportController(context);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            // Rozwiązujemy zgłoszenie i zaznaczamy addToFaq = true
            var result = await controller.ResolveTicket(
                id: 1,
                status: TicketStatus.Resolved,
                managerResponse: "Zresetuj hasło.",
                addToFaq: true,
                faqQuestion: "Jak zresetować hasło?"
            );

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            // Sprawdzamy czy zgłoszenie ma status Resolved
            var dbTicket = await context.SupportTickets.FindAsync(1);
            Assert.Equal(TicketStatus.Resolved, dbTicket.Status);

            // Sprawdzamy czy powstał wpis w FAQ
            var faqEntry = await context.FaqItems.FirstOrDefaultAsync();
            Assert.NotNull(faqEntry);
            Assert.Equal("Jak zresetować hasło?", faqEntry.Question);
            Assert.Equal("Zresetuj hasło.", faqEntry.Answer);
            Assert.True(faqEntry.IsPublic);
        }
    }
}