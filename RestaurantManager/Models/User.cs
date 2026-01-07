using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class User
    {
        public User()
        {
            // Inicjalizacja kolekcji, aby uniknąć NullReferenceException
            Shifts = new HashSet<Shift>();
            Reservations = new HashSet<Reservation>();
            Availabilities = new HashSet<Availability>();
        }

        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [StringLength(15)]
        [Display(Name = "Numer telefonu")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "PIN Ratunkowy")]
        [StringLength(6, MinimumLength = 4, ErrorMessage = "PIN musi mieć 4-6 znaków")]
        public string? RecoveryPin { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        public string? ProfilePicturePath { get; set; }

        // Relacje
        public virtual Employee? Employee { get; set; }

        public virtual ICollection<Reservation> Reservations { get; set; }
        public virtual ICollection<Availability> Availabilities { get; set; }
        public virtual ICollection<Shift> Shifts { get; set; }
    }
}