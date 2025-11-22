using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class ContactInfo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa restauracji jest wymagana.")]
        [Display(Name = "Nazwa Restauracji")]
        public string RestaurantName { get; set; }

        [Required(ErrorMessage = "Adres jest wymagany.")]
        [Display(Name = "Ulica i numer")]
        public string AddressStreet { get; set; }

        [Required(ErrorMessage = "Miasto jest wymagane.")]
        [Display(Name = "Miasto")]
        public string AddressCity { get; set; }

        [Required(ErrorMessage = "Kod pocztowy jest wymagany.")]
        [Display(Name = "Kod pocztowy")]
        public string AddressZipCode { get; set; }

        [Required(ErrorMessage = "Telefon jest wymagany.")]
        [Phone]
        [Display(Name = "Numer telefonu")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress]
        [Display(Name = "Email kontaktowy")]
        public string ContactEmail { get; set; }

        [Display(Name = "Link do Map Google (opcjonalnie)")]
        public string? GoogleMapsLink { get; set; }
    }
}