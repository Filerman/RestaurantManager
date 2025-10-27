using System;
using System.Collections.Generic; // Dodaj ten using
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    // Reprezentuje pojedynczą, przypisaną zmianę dla pracownika w ramach grafiku
    public class Shift : IValidatableObject // Zmienione dla walidacji
    {
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; } // FK do Schedule
        public virtual Schedule Schedule { get; set; }

        // *** POPRAWKA: UserId jest teraz opcjonalne ***
        [Display(Name = "Pracownik")]
        public int? UserId { get; set; } // FK do User (który jest Employee)
        [ForeignKey("UserId")]
        public virtual User? EmployeeUser { get; set; } // Też nullable

        [Required(ErrorMessage = "Data zmiany jest wymagana.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Godzina rozpoczęcia")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Godzina zakończenia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Godzina zakończenia")]
        public TimeSpan EndTime { get; set; }

        // Tag stanowiska pozostaje opcjonalny
        [Display(Name = "Stanowisko na zmianie")]
        public int? PositionTagId { get; set; } // FK do PositionTag
        [ForeignKey("PositionTagId")]
        public virtual PositionTag? ShiftPositionTag { get; set; }

        // Implementacja IValidatableObject do walidacji godzin
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.",
                    new[] { nameof(EndTime) });
            }
        }
    }
}