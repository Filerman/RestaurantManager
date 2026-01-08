using Microsoft.AspNetCore.Http;
using RestaurantManager.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class HomeContentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [Display(Name = "Tytuł Powitalny")]
        public string HeroTitle { get; set; }

        [Required(ErrorMessage = "Podtytuł jest wymagany")]
        [Display(Name = "Podtytuł")]
        public string HeroSubtitle { get; set; }

        [Display(Name = "Dodaj nowe zdjęcia do karuzeli")]
        public List<IFormFile>? NewImages { get; set; }

        public List<CarouselImage> ExistingImages { get; set; } = new List<CarouselImage>();
    }
}