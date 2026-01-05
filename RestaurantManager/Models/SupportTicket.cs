using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public enum TicketStatus
    {
        [Display(Name = "Nowe")] New,
        [Display(Name = "W toku")] InProgress,
        [Display(Name = "Rozwiązane")] Resolved,
        [Display(Name = "Odrzucone")] Rejected
    }

    public class SupportTicket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Temat jest wymagany")]
        [Display(Name = "Temat")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany")]
        [Display(Name = "Opis problemu")]
        public string Description { get; set; }

        [Display(Name = "Status")]
        public TicketStatus Status { get; set; } = TicketStatus.New;

        [Display(Name = "Data zgłoszenia")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [Display(Name = "Odpowiedź Managera")]
        public string? ManagerResponse { get; set; }

        public bool HasUnreadResponse { get; set; } = false;

        public int CreatedByUserId { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }
}