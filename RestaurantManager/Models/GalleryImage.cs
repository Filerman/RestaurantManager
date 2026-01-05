using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class GalleryImage
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string? Caption { get; set; }

        public bool IsInCarousel { get; set; } = false;
    }
}