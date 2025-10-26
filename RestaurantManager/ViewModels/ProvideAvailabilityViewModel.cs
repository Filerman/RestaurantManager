﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    // Używany przez Views/Availability/Provide.cshtml
    public class ProvideAvailabilityViewModel : IValidatableObject
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month;
        public List<MonthEntryViewModel> Days { get; set; }

        public ProvideAvailabilityViewModel()
        {
            Days = new List<MonthEntryViewModel>();
        }

        // Walidacja godzin (EndTime > StartTime) dla formularza Provide
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasError = false;
            foreach (var day in Days)
            {
                // Sprawdzaj tylko zaznaczone dni, które mają obiekt Availability i nie są w przeszłości
                if (day.IsSelected && day.Date >= DateTime.Today && day.Availability != null && day.Availability.EndTime <= day.Availability.StartTime)
                {
                    hasError = true;
                    // Zwróć błąd od razu po znalezieniu pierwszego problemu
                    yield return new ValidationResult(
                        $"Błędne godziny dla {day.Date:yyyy-MM-dd}. Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.",
                        // Przypisanie błędu do konkretnego elementu listy jest trudne,
                        // więc przypisujemy ogólnie lub do jednego z pól czasu
                        new[] { $"Days[{Days.IndexOf(day)}].Availability.EndTime" });
                    // Można by kontynuować szukanie błędów, ale jeden wystarczy, by zatrzymać zapis
                    // break;
                }
            }
        }
    }
}