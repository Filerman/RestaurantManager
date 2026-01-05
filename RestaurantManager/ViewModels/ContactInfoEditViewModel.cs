using RestaurantManager.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RestaurantManager.ViewModels
{
    public class ContactInfoEditViewModel
    {
        public ContactInfo Contact { get; set; }
        public List<OpeningHour> OpeningHours { get; set; }

        [Display(Name = "Wybierz plik Logo")]
        public IFormFile? LogoImage { get; set; }

        public string? CroppedLogoBase64 { get; set; }
    }
}