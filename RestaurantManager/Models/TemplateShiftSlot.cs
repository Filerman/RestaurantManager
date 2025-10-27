using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    // Reprezentuje pojedyncze wymaganie w szablonie
    // Np. "W poniedziałek od 9:00 do 17:00 potrzeba 2 Kelnerów"
    public class TemplateShiftSlot
    {
        public int Id { get; set; }

        [Required]
        public int ScheduleTemplateId { get; set; } // FK do ScheduleTemplate
        public virtual ScheduleTemplate ScheduleTemplate { get; set; }

        [Required(ErrorMessage = "Dzień tygodnia jest wymagany.")]
        [Display(Name = "Dzień tygodnia")]
        public DayOfWeek DayOfWeek { get; set; } // Poniedziałek, Wtorek, etc.

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Godzina rozpoczęcia")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Godzina zakończenia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Godzina zakończenia")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Display(Name = "Wymagany Tag Stanowiska")]
        public int PositionTagId { get; set; } // FK do PositionTag (np. Kelner, Kuchnia)
        [ForeignKey("PositionTagId")]
        public virtual PositionTag RequiredPositionTag { get; set; }

        [Required(ErrorMessage = "Liczba pracowników jest wymagana.")]
        [Range(1, 100, ErrorMessage = "Liczba pracowników musi być co najmniej 1.")]
        [Display(Name = "Wymagana liczba pracowników")]
        public int RequiredEmployeeCount { get; set; } = 1;

        // Walidacja sprawdzająca, czy EndTime jest po StartTime
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