using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class Employee
    {
        public Employee()
        {
            PositionTags = new HashSet<PositionTag>();
        }

        [Key]
        public int UserId { get; set; }

        public virtual User? User { get; set; }

        [Required, StringLength(60)]
        [Display(Name = "Imię i nazwisko")]
        public string FullName { get; set; }

        [Required]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Numer telefonu musi mieć 9 cyfr.")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; }

        // USUNIĘTO: public decimal? HourlyRate { get; set; } 
        // Stawka jest teraz pobierana ze zmiany (Shift -> PositionTag -> HourlyRate)

        public virtual ICollection<PositionTag> PositionTags { get; set; }
    }
}