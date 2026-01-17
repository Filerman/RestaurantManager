using Microsoft.AspNetCore.Mvc;
using Moq;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Encodings.Web; // Ważne dla polskich znaków
using Xunit;

namespace RestaurantManager.Tests
{
    public class SchedulesControllerTests
    {
        [Fact]
        public async Task Create_Post_ReturnsError_WhenScheduleOverlaps()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();
            // Istniejący grafik: 1-7 Stycznia 2026
            context.Schedules.Add(new Schedule { StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 7) });
            await context.SaveChangesAsync();

            var controller = new SchedulesController(context);
            // Nowy grafik nachodzący: 5-10 Stycznia 2026
            var newSchedule = new ScheduleCreateViewModel { StartDate = new DateTime(2026, 1, 5), EndDate = new DateTime(2026, 1, 10) };

            // Act
            var result = await controller.Create(newSchedule);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task SaveShift_ReturnsError_WhenUserHasShiftConflict()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // Kontekst: Użytkownik, Pracownik, Tag, Grafik
            var user = new User { Id = 1, Username = "user", Email = "u@u.pl", Password = "123", Role = "Employee" };
            var employee = new Employee
            {
                UserId = 1,
                FullName = "Test Emp",
                Phone = "123",
                HireDate = DateTime.Now,
                PositionTags = new[] { new PositionTag { Id = 5, Name = "Waiter", HourlyRate = 20 } }
            };
            var schedule = new Schedule { Id = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(7) };

            // Dostępność pracownika (żeby przeszedł walidację dostępności)
            context.Availabilities.Add(new Availability { UserId = 1, Date = DateTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0) });

            // ISTNIEJĄCA ZMIANA: 10:00 - 14:00
            context.Shifts.Add(new Shift
            {
                ScheduleId = 1,
                UserId = 1,
                Date = DateTime.Today,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                PositionTagId = 5
            });

            context.Users.Add(user);
            context.Employees.Add(employee);
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();

            var controller = new SchedulesController(context);

            // Act - Próba dodania zmiany 12:00 - 16:00 (Konflikt z 10-14)
            var conflictShift = new ShiftViewModel
            {
                Id = 0,
                ScheduleId = 1,
                Date = DateTime.Today,
                StartTime = new TimeSpan(12, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                AssignedUserId = 1,
                PositionTagId = 5
            };

            var result = await controller.SaveShift(conflictShift);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonString = JsonSerializer.Serialize(jsonResult.Value, options);

            Assert.Contains("false", jsonString);
            Assert.Contains("kolidującą", jsonString);
        }
    }
}