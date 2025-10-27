using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class TemplateShiftSlotViewModel
    {
        public int Id { get; set; } // 0 dla nowego slotu

        [Required(ErrorMessage = "Dzień tygodnia jest wymagany.")]
        [Display(Name = "Dzień")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0); // Domyślna wartość

        [Required(ErrorMessage = "Godzina zakończenia jest wymagana.")]
        [DataType(DataType.Time)]
        [Display(Name = "Koniec")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0); // Domyślna wartość

        [Required(ErrorMessage = "Tag stanowiska jest wymagany.")]
        [Display(Name = "Tag")]
        public int PositionTagId { get; set; }

        [Required(ErrorMessage = "Liczba pracowników jest wymagana.")]
        [Range(1, 100)]
        [Display(Name = "Ilość")]
        public int RequiredEmployeeCount { get; set; } = 1;

        // Flaga do obsługi usuwania slotów w formularzu
        public bool IsMarkedForDeletion { get; set; } = false;

        // Walidacja przeniesiona tutaj dla ViewModelu
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsMarkedForDeletion && EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "Koniec musi być po początku.",
                    new[] { nameof(EndTime) });
            }
        }
    }
}