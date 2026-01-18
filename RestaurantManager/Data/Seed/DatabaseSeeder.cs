using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;

namespace RestaurantManager.Data
{
    public static class DatabaseSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Migracja bazy danych (utworzy nową, jeśli zmienisz nazwę w appsettings.json)
            context.Database.Migrate();

            // 1. Użytkownicy (Role + Gość)
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    // Admin
                    new User {
                        Username = "admin",
                        Email = "admin@r.com",
                        Password = "admin123",
                        Role = "Admin",
                        ProfilePicturePath = "/images/profiles/default.png",
                        PhoneNumber = "503123456"
                    },
                    // Manager
                    new User {
                        Username = "manager",
                        Email = "mgr@r.com",
                        Password = "manager123",
                        Role = "Manager",
                        ProfilePicturePath = "/images/profiles/default.png",
                        PhoneNumber = "502123456"
                    },
                    // Pracownik
                    new User {
                        Username = "employee",
                        Email = "empl@r.com",
                        Password = "emp123",
                        Role = "Employee",
                        ProfilePicturePath = "/images/profiles/default.png",
                        PhoneNumber = "501123456"

                    },
                    // Gość (Klient do prezentacji zamówień)
                    new User {
                        Username = "guest",
                        Email = "guest@r.pl",
                        Password = "guest123",
                        Role = "Guest",
                        ProfilePicturePath = "/images/profiles/default.png",
                        PhoneNumber = "500123456"
                    }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // 2. Dane kontaktowe
            if (!context.ContactInfos.Any())
            {
                context.ContactInfos.Add(new ContactInfo
                {
                    RestaurantName = "Modern Bistro",
                    PhoneNumber = "123-456-789",
                    ContactEmail = "kontakt@modernbistro.pl",
                    AddressStreet = "ul. Prezentacyjna 15",
                    AddressCity = "Warszawa",
                    AddressZipCode = "00-001",
                    EstimatedDeliveryTimeMinutes = 45,
                    AvailabilityDeadlineDays = 7,
                    DefaultTableOccupancyMinutes = 90
                });
            }

            // 3. Stoliki
            if (!context.Tables.Any())
            {
                context.Tables.AddRange(
                    new Table { Name = "Stolik 1 (Okno)", Capacity = 2, IsAvailable = true },
                    new Table { Name = "Stolik 2 (Centrum)", Capacity = 4, IsAvailable = true },
                    new Table { Name = "Stolik 3 (Rodzinny)", Capacity = 6, IsAvailable = true },
                    new Table { Name = "Stolik 4 (Ustronny)", Capacity = 2, IsAvailable = true },
                    new Table { Name = "Loża VIP", Capacity = 8, IsAvailable = true },
                    new Table { Name = "Stolik 6 (Bar)", Capacity = 2, IsAvailable = false }
                );
            }

            // 4. Menu
            if (!context.MenuItems.Any())
            {
                context.MenuItems.AddRange(
                     // Zupy
                     new MenuItem { Name = "Rosół Domowy", Description = "Tradycyjny rosół na trzech mięsach z domowym makaronem.", Price = 18.00m, Category = "Zupy", IsAvailable = true },
                     new MenuItem { Name = "Krem z Pomidorów", Description = "Gęsty krem z pieczonych pomidorów z bazylią (Wegański).", Price = 22.00m, Category = "Zupy", IsAvailable = true },

                     // Dania Główne
                     new MenuItem { Name = "Schabowy Gigant", Description = "Tradycyjny kotlet schabowy, ziemniaki z koperkiem, zasmażana kapusta.", Price = 42.00m, Category = "Dania Główne", IsAvailable = true },
                     new MenuItem { Name = "Burger Wołowy", Description = "100% wołowina, cheddar, bekon, sos BBQ, frytki steakhouse.", Price = 39.00m, Category = "Dania Główne", IsAvailable = true },
                     new MenuItem { Name = "Pierogi Ruskie", Description = "Ręcznie lepione pierogi z okrasą (8 szt.).", Price = 28.00m, Category = "Dania Główne", IsAvailable = true },
                     new MenuItem { Name = "Bowl Wegański", Description = "Komosa ryżowa, awokado, tofu wędzone, warzywa sezonowe, sos orzechowy.", Price = 36.00m, Category = "Dania Główne", IsAvailable = true },

                     // Desery
                     new MenuItem { Name = "Sernik Nowojorski", Description = "Kremowy sernik na kruchym spodzie z sosem malinowym.", Price = 24.00m, Category = "Desery", IsAvailable = true },
                     new MenuItem { Name = "Szarlotka na Ciepło", Description = "Z lodami waniliowymi i bitą śmietaną.", Price = 20.00m, Category = "Desery", IsAvailable = true },

                     // Napoje
                     new MenuItem { Name = "Lemoniada Cytrusowa", Description = "Świeżo wyciskana z miętą (0.4l).", Price = 15.00m, Category = "Napoje", IsAvailable = true },
                     new MenuItem { Name = "Kawa Latte", Description = "Aromatyczna kawa z dużą ilością mleka.", Price = 16.00m, Category = "Napoje", IsAvailable = true }
                );
            }

            // 5. Godziny otwarcia
            if (!context.OpeningHours.Any())
            {
                var days = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
                foreach (var day in days)
                {
                    bool isWeekend = (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday);
                    context.OpeningHours.Add(new OpeningHour
                    {
                        DayOfWeek = day,
                        OpenTime = new TimeSpan(12, 0, 0),
                        CloseTime = isWeekend ? new TimeSpan(23, 0, 0) : new TimeSpan(22, 0, 0),
                        IsClosed = false
                    });
                }
            }

            // 6. Ogłoszenia
            if (!context.Announcements.Any())
            {
                context.Announcements.AddRange(
                    new Announcement
                    {
                        Title = "Wielkie Otwarcie!",
                        Content = "Już teraz zapraszamy do naszego odświeżonego lokalu. Czekają na Was nowe smaki i promocje na start!",
                        ValidFrom = DateTime.Now.AddDays(-2),
                        ValidUntil = DateTime.Now.AddDays(14),
                        DateCreated = DateTime.Now.AddDays(-2)
                    },
                    new Announcement
                    {
                        Title = "Nowe Menu Sezonowe",
                        Content = "Wprowadziliśmy do karty dania ze świeżych, sezonowych warzyw. Sprawdź naszą ofertę wegańską!",
                        ValidFrom = DateTime.Now.AddDays(-1),
                        ValidUntil = DateTime.Now.AddDays(30),
                        DateCreated = DateTime.Now.AddDays(-1)
                    }
                );
            }

            // 7. FAQ
            if (!context.FaqItems.Any())
            {
                context.FaqItems.AddRange(
                    new FaqItem { Question = "Czy macie opcje wegańskie?", Answer = "Tak, posiadamy dedykowane pozycje w menu, np. Bowl Wegański czy Krem z Pomidorów.", IsPublic = true },
                    new FaqItem { Question = "Czy realizujecie dostawy?", Answer = "Tak, dowozimy jedzenie na terenie całego miasta.", IsPublic = true },
                    new FaqItem { Question = "Czy można płacić kartą przy odbiorze?", Answer = "Oczywiście, nasi dostawcy posiadają terminale płatnicze.", IsPublic = true },
                    new FaqItem { Question = "Jak zgłosić urlop? (Pracownicy)", Answer = "Wnioski urlopowe składamy przez panel pracowniczy z wyprzedzeniem 14 dni.", IsPublic = false }
                );
            }

            // 8. Strefy dostaw
            if (!context.DeliveryZones.Any())
            {
                // Uwaga: ZipCodePattern jest opcjonalny w Twoim kodzie? Dodaję go dla pewności.
                context.DeliveryZones.AddRange(
                    new DeliveryZone { CityName = "Warszawa", DeliveryFee = 15.00m },
                    new DeliveryZone { CityName = "Centrum", DeliveryFee = 5.00m },
                    new DeliveryZone { CityName = "Okolice", DeliveryFee = 25.00m }
                );
            }

            // 9. Stanowiska (dla grafiku)
            if (!context.PositionTags.Any())
            {
                context.PositionTags.AddRange(
                    new PositionTag { Name = "Kelner", HourlyRate = 25.00m },
                    new PositionTag { Name = "Kucharz", HourlyRate = 35.00m },
                    new PositionTag { Name = "Barman", HourlyRate = 28.00m }
                );
            }

            context.SaveChanges();
        }
    }
}