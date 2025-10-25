using System.Linq;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;

namespace RestaurantManager.Data
{
    public static class DatabaseSeeder
    {
        public static void Seed(AppDbContext context)
        {
            context.Database.Migrate();

            // Admin
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    Email = "admin@r.com",
                    Password = "admin123",
                    Role = "Admin"
                });
            }

            // Manager
            if (!context.Users.Any(u => u.Username == "manager"))
            {
                context.Users.Add(new User
                {
                    Username = "manager",
                    Email = "mgr@r.com",
                    Password = "manager123",
                    Role = "Manager"
                });
            }

            // Pracownik
            if (!context.Users.Any(u => u.Username == "employee"))
            {
                context.Users.Add(new User
                {
                    Username = "employee",
                    Email = "empl@r.com",
                    Password = "emp123",
                    Role = "Employee"
                });
            }

            context.SaveChanges();
        }
    }
}
