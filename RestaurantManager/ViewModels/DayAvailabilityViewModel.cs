using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class DayAvailabilityViewModel
    {
        public int Day { get; set; } 
        public bool IsAvailable { get; set; } 

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }
    }
}
