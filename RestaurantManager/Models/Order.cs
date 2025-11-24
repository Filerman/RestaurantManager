using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Nowe")]
        Created,
        [Display(Name = "Zatwierdzone")]
        Accepted,
        [Display(Name = "W przygotowaniu")]
        InKitchen,
        [Display(Name = "Gotowe / W dostawie")]
        Ready,
        [Display(Name = "Zakończone")]
        Completed,
        [Display(Name = "Anulowane")]
        Canceled
    }

    public enum OrderType
    {
        [Display(Name = "Dostawa")]
        Delivery,
        [Display(Name = "Odbiór osobisty")]
        Takeaway,
        [Display(Name = "Na miejscu")]
        DineIn
    }

    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Data złożenia")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Zaplanowane na")]
        public DateTime ScheduledDate { get; set; } // Data realizacji

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; } = OrderStatus.Created;

        [Display(Name = "Typ zamówienia")]
        public OrderType Type { get; set; }

        [Display(Name = "Łączna kwota")]
        public decimal TotalAmount { get; set; }

        // Dane zamawiającego (może być zalogowany lub gość)
        public string? UserId { get; set; } // Trzymamy ID jako string (lub int zależnie od User.cs), ale user może być nullem (gość)

        [Required(ErrorMessage = "Imię i nazwisko jest wymagane")]
        [Display(Name = "Klient")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Telefon jest wymagany")]
        [Phone]
        [Display(Name = "Telefon")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; }

        [Display(Name = "Adres dostawy")]
        public string? DeliveryAddress { get; set; } // Wymagane tylko dla OrderType.Delivery

        [Display(Name = "Uwagi do zamówienia")]
        public string? Notes { get; set; }

        // Relacja: Jedno zamówienie ma wiele pozycji
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}