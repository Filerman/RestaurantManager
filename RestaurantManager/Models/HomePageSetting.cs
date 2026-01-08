using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class HomePageSetting
    {
        public int Id { get; set; }

        [Display(Name = "Tytuł główny")]
        public string HeroTitle { get; set; } = "Najlepsze smaki w mieście.";

        [Display(Name = "Podtytuł")]
        public string HeroSubtitle { get; set; } = "Odkryj nasze menu i zarezerwuj stolik na wyjątkowy wieczór.";

        public virtual ICollection<CarouselImage> CarouselImages { get; set; } = new List<CarouselImage>();
    }
}