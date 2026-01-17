using RestaurantManager.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class AvailabilityViewModel
    {
        public List<Availability> Availabilities { get; set; }

        [Display(Name = "Nowa Dostępność")]
        public Availability NewAvailability { get; set; }
    }
}