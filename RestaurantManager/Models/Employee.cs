using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string FullName { get; set; }

        [Required, StringLength(40)]
        public string Position { get; set; }          // np. Waiter, Chef

        [Required]
        [RegularExpression(@"^\d{9}$",
            ErrorMessage = "Phone number must be exactly 9 digits.")]
        public string Phone { get; set; }

        public decimal? HourlyRate { get; set; }      // opcjonalnie
    }
}
