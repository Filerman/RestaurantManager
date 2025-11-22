using RestaurantManager.Models;
using System.Collections.Generic;

namespace RestaurantManager.ViewModels
{
    public class ContactInfoEditViewModel
    {
        public ContactInfo Contact { get; set; }
        public List<OpeningHour> OpeningHours { get; set; }
    }
}