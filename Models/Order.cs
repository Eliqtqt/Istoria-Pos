using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public string? DeliveryAddress { get; set; }

        public string? Notes { get; set; }

        // ✅ Prevents null validation error
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
