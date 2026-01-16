using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Dodano dla Column Type

namespace RestaurantManager.Models
{
    public class PositionTag
    {
        public PositionTag()
        {
            Employees = new HashSet<Employee>();
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa stanowiska jest wymagana.")]
        [StringLength(50)]
        [Display(Name = "Nazwa stanowiska")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Stawka godzinowa jest wymagana.")]
        [Range(0, 10000, ErrorMessage = "Stawka musi być dodatnia.")]
        [Display(Name = "Stawka godzinowa (PLN)")]
        [Column(TypeName = "decimal(18,2)")] 
        public decimal HourlyRate { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
    }
}