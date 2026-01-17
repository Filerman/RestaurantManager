using Microsoft.AspNetCore.Mvc;
using RestaurantManager.Controllers;
using RestaurantManager.Models;
using RestaurantManager.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantManager.Tests
{
    public class EmployeesControllerTests
    {
        [Fact]
        public async Task Payroll_CalculatesTotalSalary_Correctly()
        {
            // Arrange
            var context = DbHelper.GetInMemoryDbContext();

            // 1. Użytkownik i Pracownik (z wymaganymi polami)
            var user = new User { Id = 1, Username = "janek", Email = "jan@test.pl", Password = "123", Role = "Employee" };
            var employee = new Employee { UserId = 1, FullName = "Jan Kowalski", Phone = "123456789", HireDate = DateTime.Now };

            // 2. Tagi stanowisk
            var tagWaiter = new PositionTag { Id = 1, Name = "Kelner", HourlyRate = 20 }; // 20 zł/h
            var tagCook = new PositionTag { Id = 2, Name = "Kucharz", HourlyRate = 40 };  // 40 zł/h

            context.Users.Add(user);
            context.Employees.Add(employee);
            context.PositionTags.AddRange(tagWaiter, tagCook);

            // 3. Zmiany w grafiku
            var payrollDate = new DateTime(2026, 6, 1);

            // Zmiana 1: 5 godzin jako Kelner (5h * 20zł = 100zł)
            context.Shifts.Add(new Shift
            {
                UserId = 1,
                Date = payrollDate,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                PositionTagId = 1,
                EmployeeUser = user,
                ShiftPositionTag = tagWaiter
            });

            // Zmiana 2: 5 godzin jako Kucharz (5h * 40zł = 200zł)
            context.Shifts.Add(new Shift
            {
                UserId = 1,
                Date = payrollDate.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                PositionTagId = 2,
                EmployeeUser = user,
                ShiftPositionTag = tagCook
            });

            await context.SaveChangesAsync();

            var controller = new EmployeesController(context);

            // Act
            var result = await controller.Payroll(6, 2026);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PayrollIndexViewModel>(viewResult.Model);
            var employeePayroll = model.Payrolls.First(p => p.EmployeeName == "Jan Kowalski");

            // Oczekiwane: 100 + 200 = 300 PLN
            Assert.Equal(300m, employeePayroll.TotalSalary);
            Assert.Equal(10.0, employeePayroll.TotalHours);
        }
    }
}