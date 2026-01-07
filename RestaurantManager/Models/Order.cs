using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public enum PaymentMethod
    {
        [Display(Name = "Gotówka")]
        Cash,
        [Display(Name = "Karta (Terminal)")]
        Card
    }

    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Data złożenia")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Zaplanowane na")]
        public DateTime ScheduledDate { get; set; }

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; } = OrderStatus.Created;

        [Display(Name = "Typ zamówienia")]
        public OrderType Type { get; set; }

        [Display(Name = "Płatność")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Display(Name = "Łączna kwota")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? UserId { get; set; }

        // Pola klienta mogą być puste przy zamówieniu "Na miejscu" (DineIn)
        [Display(Name = "Klient")]
        public string? CustomerName { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string? CustomerPhone { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? CustomerEmail { get; set; }

        [Display(Name = "Adres dostawy")]
        public string? DeliveryAddress { get; set; }

        [Display(Name = "Uwagi do zamówienia")]
        public string? Notes { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [Display(Name = "Stolik")]
        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public virtual Table? Table { get; set; }
    }
}