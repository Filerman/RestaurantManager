using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Data rozpoczęcia grafiku jest wymagana.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Data zakończenia grafiku jest wymagana.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data zakończenia")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Opublikowany")]
        public bool IsPublished { get; set; } = false; 

        [Display(Name = "Data utworzenia")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Data zakończenia musi być taka sama lub późniejsza niż data rozpoczęcia.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}