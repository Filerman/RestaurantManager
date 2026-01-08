using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class LossLog
    {
        public int Id { get; set; }


        [Display(Name = "Pozycja z Menu")]
        public int? MenuItemId { get; set; }

        [ForeignKey("MenuItemId")]
        public virtual MenuItem? MenuItem { get; set; }

        [Display(Name = "Nazwa (spoza menu)")]
        public string? CustomItemName { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Koszt jedn.")]
        public decimal? CustomItemCost { get; set; }

        [Required(ErrorMessage = "Ilość jest wymagana.")]
        [Range(1, 1000, ErrorMessage = "Ilość musi być większa od 0.")]
        [Display(Name = "Ilość")]
        public int Quantity { get; set; } = 1;


        [Required(ErrorMessage = "Opis jest wymagany.")]
        [Display(Name = "Opis szczegółowy")]
        [StringLength(500, ErrorMessage = "Opis nie może przekraczać 500 znaków.")]
        public string Description { get; set; } // Np. "Upadło na podłogę"

        [Required(ErrorMessage = "Typ straty jest wymagany.")]
        [Display(Name = "Kategoria straty")]
        public string LossType { get; set; } // Np. "Uszkodzenie", "Zepsucie"

        // EstimatedValue teraz będzie wyliczane automatycznie (Ilość * Cena)
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Wartość całkowita (PLN)")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal EstimatedValue { get; set; }

        [Display(Name = "Data zgłoszenia")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateReported { get; set; } = DateTime.Now;

        [Required]
        public int ReportedByUserId { get; set; }

        [ForeignKey("ReportedByUserId")]
        [Display(Name = "Zgłoszony przez")]
        public virtual User? ReportedByUser { get; set; }

        public int? ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
    }
}