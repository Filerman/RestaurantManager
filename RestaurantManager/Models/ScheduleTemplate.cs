using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    // Reprezentuje szablon grafiku, np. "Standardowy tydzień", "Lato weekendy"
    public class ScheduleTemplate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa szablonu jest wymagana.")]
        [StringLength(100)]
        [Display(Name = "Nazwa szablonu")]
        public string Name { get; set; }

        // Kolekcja slotów (wymagań) zdefiniowanych w tym szablonie
        public virtual ICollection<TemplateShiftSlot> ShiftSlots { get; set; } = new List<TemplateShiftSlot>();
    }
}