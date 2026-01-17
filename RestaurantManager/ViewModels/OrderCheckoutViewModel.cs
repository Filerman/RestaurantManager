using RestaurantManager.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.ViewModels
{
    public class OrderCheckoutViewModel
    {
        [Required(ErrorMessage = "Imię i nazwisko jest wymagane")]
        [Display(Name = "Imię i nazwisko")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Telefon jest wymagany")]
        [Phone]
        [Display(Name = "Numer telefonu")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        public string CustomerEmail { get; set; }

        [Display(Name = "Ulica i numer")]
        public string? DeliveryStreet { get; set; }

        [Display(Name = "Kod pocztowy")]
        [RegularExpression(@"^\d{2}-\d{3}$", ErrorMessage = "Kod pocztowy musi mieć format XX-XXX")]
        public string? DeliveryZipCode { get; set; }

        [Display(Name = "Miasto")]
        public string? DeliveryCity { get; set; }

        [Display(Name = "Typ zamówienia")]
        public OrderType OrderType { get; set; }

        [Required(ErrorMessage = "Wybierz sposób płatności")]
        [Display(Name = "Sposób płatności")]
        public PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Jak najszybciej")]
        public bool IsAsap { get; set; } = true;

        [Display(Name = "Data realizacji")]
        public DateTime ScheduledDate { get; set; } = DateTime.Now;

        [Display(Name = "Uwagi do zamówienia")]
        public string? Notes { get; set; }

        public decimal TotalAmount { get; set; }
    }
}