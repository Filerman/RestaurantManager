using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class ScheduleCreateViewModel
    {
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data rozpoczęcia grafiku")]
        public DateTime StartDate { get; set; } = DateTime.Today; // Domyślnie dzisiaj

        [Required(ErrorMessage = "Data zakończenia jest wymagana.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data zakończenia grafiku")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(6); // Domyślnie tydzień od dzisiaj

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.",
                    new[] { nameof(EndDate) });
            }
            if ((EndDate - StartDate).TotalDays > 90) // Ograniczenie długości grafiku
            {
                yield return new ValidationResult(
                    "Grafik nie może obejmować więcej niż 90 dni.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}