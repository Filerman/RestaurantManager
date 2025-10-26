// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        // ścieżka do pliku w wwwroot/images/profiles
        public string? ProfilePicturePath { get; set; }

        // Relacja 1:1 - Użytkownik może (ale nie musi) być pracownikiem
        public virtual Employee? Employee { get; set; }
    }
}