using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class ProvideAvailabilityViewModel
    {
        [Required] public int Year { get; set; }
        [Required] public int Month { get; set; }
    }
}
