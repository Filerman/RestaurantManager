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
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public int TableId { get; set; }
        public virtual Table? Table { get; set; }

        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}