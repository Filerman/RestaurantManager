using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class GalleryImage
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } // Ścieżka do pliku (np. /images/gallery/obrazek.jpg)

        public string? Caption { get; set; } // Opcjonalny opis
    }
}