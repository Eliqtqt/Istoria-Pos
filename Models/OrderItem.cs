using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        public MenuItem? MenuItem { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
    }
}