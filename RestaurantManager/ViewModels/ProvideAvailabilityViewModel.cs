using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class ProvideAvailabilityViewModel : IValidatableObject
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month;
        public List<MonthEntryViewModel> Days { get; set; }

        public ProvideAvailabilityViewModel()
        {
            Days = new List<MonthEntryViewModel>();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasError = false;
            foreach (var day in Days)
            {
                if (day.IsSelected && day.Date >= DateTime.Today && day.Availability != null && day.Availability.EndTime <= day.Availability.StartTime)
                {
                    hasError = true;
                    yield return new ValidationResult(
                        $"Błędne godziny dla {day.Date:yyyy-MM-dd}. Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.",
                        new[] { $"Days[{Days.IndexOf(day)}].Availability.EndTime" });
                }
            }
        }
    }
}