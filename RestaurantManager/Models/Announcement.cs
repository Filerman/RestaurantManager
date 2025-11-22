using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [Display(Name = "Tytuł")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Treść jest wymagana")]
        [Display(Name = "Treść")]
        public string Content { get; set; }

        // Data utworzenia (dla celów informacyjnych/auditowych)
        [Display(Name = "Data utworzenia")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // *** NOWOŚĆ: Ważne OD ***
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        [Display(Name = "Widoczne od")]
        public DateTime ValidFrom { get; set; } = DateTime.Now;

        // Ważne DO
        [Display(Name = "Ważne do (opcjonalne)")]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Zdjęcie")]
        public string? ImagePath { get; set; }
    }
}