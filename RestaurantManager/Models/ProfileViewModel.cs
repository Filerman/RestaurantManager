using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class ProfileViewModel
    {
        // Ukryte w formularzu, ale wymagane dla model bindera:
        public string Username { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Display(Name = "Profile picture")]
        public IFormFile? ProfileImage { get; set; }

        public string? ExistingPicturePath { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string? OldPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords must match")]
        [Display(Name = "Confirm new password")]
        public string? ConfirmPassword { get; set; }
    }
}
