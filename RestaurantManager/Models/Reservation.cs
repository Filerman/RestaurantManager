using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // DODAJ TO

namespace RestaurantManager.Models
{
    public class Reservation : IValidatableObject
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string CustomerName { get; set; }

        // 9 cyfr – tylko cyfry
        [Required]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Phone number must be exactly 9 digits.")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "Date & time")]
        public DateTime DateTime { get; set; }

        [Range(1, 20)]
        [Display(Name = "Party size")]
        public int PartySize { get; set; }

        public string? Notes { get; set; }

        // --- NOWO DODANE POLA ---

        [Display(Name = "Table")]
        public int? TableId { get; set; } // Klucz obcy (nullable)

        [ForeignKey("TableId")]
        public virtual Table? Table { get; set; } // Właściwość nawigacyjna

        // data nie może  być w przeszłości
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateTime < DateTime.Now)
            {
                yield return new ValidationResult(
                    "Reservation date must be in the future.",
                    new[] { nameof(DateTime) });
            }
        }
    }
}