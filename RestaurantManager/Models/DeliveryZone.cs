using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class DeliveryZone
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa miejscowości jest wymagana")]
        [Display(Name = "Miejscowość")]
        public string CityName { get; set; }

        [Required]
        [Display(Name = "Opłata za dowóz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; }
    }
}