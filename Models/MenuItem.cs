using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public string Category { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
    
        public bool IsVegetarian { get; set; }
    
        public bool IsVegan { get; set; }
    
        public bool IsGlutenFree { get; set; }
    
        public double Rating { get; set; } = 0;
    
        public int ReviewCount { get; set; } = 0;
    
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
