using RestaurantManager.Models;
using System;

namespace RestaurantManager.ViewModels
{
    // Używany przez ProvideAvailabilityViewModel i AvailabilityController
    public class MonthEntryViewModel
    {
        public DateTime Date { get; set; }
        public bool IsInCurrentMonth { get; set; }
        public Availability? Availability { get; set; } // Może być null
        public bool IsSelected { get; set; } // Dla checkboxa
    }
}