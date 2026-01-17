using RestaurantManager.Models;
using System;

namespace RestaurantManager.ViewModels
{
    public class MonthEntryViewModel
    {
        public DateTime Date { get; set; }
        public bool IsInCurrentMonth { get; set; }
        public Availability? Availability { get; set; }
        public bool IsSelected { get; set; } 
    }
}