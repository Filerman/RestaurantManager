using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RestaurantManager.Models; // Potrzebne dla ScheduleTemplate

namespace RestaurantManager.ViewModels
{
    public class ScheduleTemplateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa szablonu jest wymagana.")]
        [StringLength(100)]
        [Display(Name = "Nazwa szablonu")]
        public string Name { get; set; }

        // Lista slotów do wyświetlenia/edycji w formularzu
        public List<TemplateShiftSlotViewModel> Slots { get; set; } = new List<TemplateShiftSlotViewModel>();

        // Opcjonalnie: Lista dla nowych slotów, jeśli chcesz je oddzielić w UI
        // public List<TemplateShiftSlotViewModel> NewSlots { get; set; } = new List<TemplateShiftSlotViewModel>();
    }
}