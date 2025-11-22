using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class AnnouncementViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [Display(Name = "Tytuł")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Treść jest wymagana")]
        [Display(Name = "Treść")]
        public string Content { get; set; }

        // *** NOWOŚĆ ***
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        [Display(Name = "Widoczne od")]
        public DateTime ValidFrom { get; set; } = DateTime.Now;

        [Display(Name = "Ważne do (opcjonalne)")]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Zdjęcie (opcjonalne)")]
        public IFormFile? Image { get; set; }

        public string? ExistingImagePath { get; set; }
    }
}