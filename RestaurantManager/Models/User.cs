using System.Collections.Generic; // Dodaj ten using
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
        public string Password { get; set; } // Pamiętaj, że to czysty tekst

        [Required]
        public string Role { get; set; }

        public string? ProfilePicturePath { get; set; }

        // Relacja 1:1 - User może być Employee
        public virtual Employee? Employee { get; set; }

        // Relacja 1:N - User może mieć wiele Reservations
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Relacja 1:N - User może mieć wiele Availabilities
        public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();

        // *** NOWA RELACJA 1:N - User (jako pracownik) może mieć wiele Shifts ***
        public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    }
}