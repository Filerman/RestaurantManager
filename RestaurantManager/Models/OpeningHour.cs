using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class OpeningHour
    {
        public int Id { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; } // Niedziela = 0, Poniedziałek = 1 itd.

        public TimeSpan OpenTime { get; set; } // np. 12:00
        public TimeSpan CloseTime { get; set; } // np. 22:00

        public bool IsClosed { get; set; } // Czy w ten dzień w ogóle zamknięte
    }
}