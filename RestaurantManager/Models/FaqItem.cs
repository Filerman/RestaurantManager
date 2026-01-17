using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class FaqItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pytanie jest wymagane")]
        [Display(Name = "Pytanie")]
        public string Question { get; set; }

        [Required(ErrorMessage = "Odpowiedź jest wymagana")]
        [Display(Name = "Odpowiedź")]
        public string Answer { get; set; }

        [Display(Name = "Widoczne dla klientów?")]
        public bool IsPublic { get; set; } = true;
    }
}