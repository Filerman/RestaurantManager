using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class AvailabilityViewModel
    {
        [Required] public int Year { get; set; }
        [Required] public int Month { get; set; }

        public List<DayAvailabilityViewModel> Days { get; set; } = new();
    }
}
