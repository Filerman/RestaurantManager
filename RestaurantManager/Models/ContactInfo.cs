using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public class ContactInfo
    {
        public int Id { get; set; }

        [Display(Name = "Nazwa Restauracji (Nagłówek)")]
        public string? RestaurantName { get; set; }

        [Display(Name = "Logo")]
        public string? LogoPath { get; set; }
        [Display(Name = "Pokaż w nagłówku")]
        public bool ShowLogoInHeader { get; set; } = false;

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


        [Display(Name = "Średni czas realizacji (minuty)")]
        [Range(15, 240, ErrorMessage = "Czas musi wynosić od 15 do 240 minut")]
        public int EstimatedDeliveryTimeMinutes { get; set; } = 45;

        [Display(Name = "Termin zgłaszania (dni przed końcem miesiąca)")]
        [Range(1, 31, ErrorMessage = "Wartość musi wynosić od 1 do 31 dni.")]
        public int AvailabilityDeadlineDays { get; set; } = 7;

        [Display(Name = "Domyślny czas rezerwacji (minuty)")]
        [Range(30, 480)]
        public int DefaultTableOccupancyMinutes { get; set; } = 120;
    }
}