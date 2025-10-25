using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class DayAvailabilityViewModel
    {
        public int Day { get; set; }          // 1-31
        public bool IsAvailable { get; set; } // checkbox

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }
    }
}
