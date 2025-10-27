using System.Linq;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;

namespace RestaurantManager.Data
{
    public static class DatabaseSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // *** TYMCZASOWO ZAKOMENTOWANE NA CZAS NAPRAWY MIGRACJI ***
             context.Database.Migrate();
            // *** KONIEC KOMENTOWANIA ***

            // Admin
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    Email = "admin@r.com",
                    Password = "admin123", // Pamiętaj o hashowaniu w prawdziwej aplikacji!
                    Role = "Admin",
                    ProfilePicturePath = "/images/profiles/default.png" // Dodano domyślny avatar
                });
            }

            // Manager
            if (!context.Users.Any(u => u.Username == "manager"))
            {
                context.Users.Add(new User
                {
                    Username = "manager",
                    Email = "mgr@r.com",
                    Password = "manager123", // Pamiętaj o hashowaniu!
                    Role = "Manager",
                    ProfilePicturePath = "/images/profiles/default.png" // Dodano domyślny avatar
                });
            }

            // Pracownik
            if (!context.Users.Any(u => u.Username == "employee"))
            {
                context.Users.Add(new User
                {
                    Username = "employee",
                    Email = "empl@r.com",
                    Password = "emp123", // Pamiętaj o hashowaniu!
                    Role = "Employee",
                    ProfilePicturePath = "/images/profiles/default.png" // Dodano domyślny avatar
                });
            }
            // Seed Tables if none exist - Zakładam, że ten kod jest w nowszej wersji Seeder.cs
            if (!context.Tables.Any())
            {
                context.Tables.AddRange(
                    new Table { Name = "Stolik 1", Capacity = 4, IsAvailable = true },
                    new Table { Name = "Stolik 2", Capacity = 2, IsAvailable = true },
                    new Table { Name = "Stolik 3", Capacity = 6, IsAvailable = false },
                    new Table { Name = "Loża VIP", Capacity = 8, IsAvailable = true }
                );
            }

            // Seed MenuItems if none exist - Zakładam, że ten kod jest w nowszej wersji Seeder.cs
            if (!context.MenuItems.Any())
            {
                context.MenuItems.AddRange(
                     new MenuItem { Name = "Rosół", Description = "Klasyczny rosół z makaronem", Price = 15.00m, Category = "Zupy", IsAvailable = true },
                     new MenuItem { Name = "Schabowy", Description = "Kotlet schabowy z ziemniakami i kapustą", Price = 35.00m, Category = "Dania Główne", IsAvailable = true },
                     new MenuItem { Name = "Pierogi Ruskie", Description = "Pierogi z serem i ziemniakami", Price = 25.00m, Category = "Dania Główne", IsAvailable = true },
                     new MenuItem { Name = "Sernik", Description = "Domowy sernik na zimno", Price = 20.00m, Category = "Desery", IsAvailable = false }
                );
            }


            context.SaveChanges();
        }
    }
}