using RestaurantManager.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    // Używany przez Views/Availability/Index.cshtml
    public class AvailabilityViewModel
    {
        public List<Availability> Availabilities { get; set; }

        // Dla formularza dodawania
        [Display(Name = "Nowa Dostępność")]
        public Availability NewAvailability { get; set; }
    }
}