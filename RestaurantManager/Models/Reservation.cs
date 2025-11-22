using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public enum ReservationStatus
    {
        [Display(Name = "Oczekująca")]
        Pending,
        [Display(Name = "Zatwierdzona")]
        Confirmed,
        [Display(Name = "Odrzucona")]
        Rejected
    }

    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Data i godzina")]
        public DateTime DateTime { get; set; }

        // Dane kontaktowe (mogą być pobrane z Usera, ale warto mieć je też przy rezerwacji w razie zmian)
        [Required]
        [Display(Name = "Imię i nazwisko")]
        public string CustomerName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Telefon")]
        public string CustomerPhone { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; }

        [Display(Name = "Status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending; // Domyślnie oczekująca

        // Relacja do Stolika
        public int TableId { get; set; }
        public virtual Table? Table { get; set; }

        // *** NOWOŚĆ: Relacja do Użytkownika (kto złożył rezerwację) ***
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}