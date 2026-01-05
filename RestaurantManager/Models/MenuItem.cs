using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Price { get; set; }

        public string? Category { get; set; }

        [Display(Name = "Czy dostępne?")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Zdjęcie")]
        public string? ImagePath { get; set; }
    }
}