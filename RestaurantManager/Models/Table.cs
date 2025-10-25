using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class Table
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa stolika jest wymagana.")]
        [Display(Name = "Nazwa stolika")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Pojemność jest wymagana.")]
        [Range(1, 50, ErrorMessage = "Pojemność musi być liczbą od 1 do 50.")]
        [Display(Name = "Pojemność (liczba osób)")]
        public int Capacity { get; set; }

        [Display(Name = "Czy dostępny?")]
        public bool IsAvailable { get; set; } = true; // Domyślnie stolik jest dostępny
    }
}