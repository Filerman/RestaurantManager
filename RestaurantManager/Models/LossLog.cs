using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class LossLog
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany.")]
        [Display(Name = "Opis straty")]
        [StringLength(500, ErrorMessage = "Opis nie może przekraczać 500 znaków.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Typ straty jest wymagany.")]
        [Display(Name = "Typ straty")]
        public string LossType { get; set; } // Np. "Uszkodzenie", "Zepsucie"

        [Required(ErrorMessage = "Szacowana wartość jest wymagana.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Szacowana wartość (PLN)")]
        [Range(0.01, 10000.00, ErrorMessage = "Wartość musi być pomiędzy 0.01 a 10,000.00")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal EstimatedValue { get; set; }

        [Display(Name = "Data zgłoszenia")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateReported { get; set; } = DateTime.Now;

        // Klucz obcy do użytkownika, który zgłosił stratę
        [Required]
        public int ReportedByUserId { get; set; }

        [ForeignKey("ReportedByUserId")]
        [Display(Name = "Zgłoszony przez")]
        public virtual User ReportedByUser { get; set; }

        // Opcjonalny klucz obcy do zmiany, na której wystąpiła strata
        public int? ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
    }
}