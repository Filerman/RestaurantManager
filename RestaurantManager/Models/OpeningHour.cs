using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class OpeningHour
    {
        public int Id { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; } 

        public bool IsClosed { get; set; }
    }
}