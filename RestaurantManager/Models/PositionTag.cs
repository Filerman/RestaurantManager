using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class PositionTag
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa stanowiska jest wymagana.")]
        [StringLength(50)]
        [Display(Name = "Nazwa stanowiska/tagu")]
        public string Name { get; set; }

        // Relacja: Jeden tag może mieć wielu pracowników
        public virtual ICollection<Employee> Employees { get; set; }
    }
}