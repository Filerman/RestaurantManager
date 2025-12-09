using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // To jest potrzebne dla IFormFile
using RestaurantManager.Models;    // To jest potrzebne dla PositionTag

namespace RestaurantManager.ViewModels
{
    public class ProfileViewModel
    {
        // --- DANE DO WYŚWIETLANIA ---
        public string Username { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Role { get; set; }

        [Display(Name = "Zdjęcie profilowe")]
        public string? ProfilePicturePath { get; set; }

        // Pola pracownicze (do wyświetlania w profilu)
        public string? FullName { get; set; }
        public string? Phone { get; set; }

        [Display(Name = "Staż pracy")]
        public string? SeniorityString { get; set; }

        public DateTime? HireDate { get; set; }

        public List<PositionTag> PositionTags { get; set; } = new List<PositionTag>();

        // --- DANE DO EDYCJI (Brakowało tych pól) ---

        [DataType(DataType.Password)]
        [Display(Name = "Obecne hasło")]
        public string? OldPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są identyczne.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Zmień zdjęcie")]
        public IFormFile? ProfileImage { get; set; }
    }
}