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

        // Klucz Podstawowy (PK) i Klucz Obcy (FK) do tabeli Users
        [Key]
        public int UserId { get; set; }

        // *** POPRAWKA JEST TUTAJ (dodałem '?') ***
        // Właściwość nawigacyjna może być nullem (np. gdy nie jest załadowana)
        public virtual User? User { get; set; }


        // Te pola zostają, bo nie ma ich w modelu User
        [Required, StringLength(60)]
        [Display(Name = "Imię i nazwisko")]
        public string FullName { get; set; }

        [Required]
        [RegularExpression(@"^\d{9}$",
            ErrorMessage = "Phone number must be exactly 9 digits.")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; }

        [Display(Name = "Stawka godzinowa")]
        public decimal? HourlyRate { get; set; }

        // Relacja: Wiele do wielu z PositionTag
        public virtual ICollection<PositionTag> PositionTags { get; set; }
    }
}