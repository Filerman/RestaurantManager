using System;
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class Shift : IValidatableObject 
    {
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; } 
        public virtual Schedule Schedule { get; set; }

        [Display(Name = "Pracownik")]
        public int? UserId { get; set; } 
        [ForeignKey("UserId")]
        public virtual User? EmployeeUser { get; set; } 

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

        [Display(Name = "Stanowisko na zmianie")]
        public int? PositionTagId { get; set; } 
        [ForeignKey("PositionTagId")]
        public virtual PositionTag? ShiftPositionTag { get; set; }

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