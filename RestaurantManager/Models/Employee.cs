using System; // Ważne dla DateTime
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
            HireDate = DateTime.Now; // Domyślnie dzisiaj
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

        // *** NOWOŚĆ: Data zatrudnienia ***
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data zatrudnienia")]
        public DateTime HireDate { get; set; }

        public virtual ICollection<PositionTag> PositionTags { get; set; }
    }
}