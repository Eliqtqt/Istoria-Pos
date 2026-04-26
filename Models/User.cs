using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = "Customer"; // Customer or Admin

        public bool EmailConfirmed { get; set; } = false;

        public string? EmailConfirmationToken { get; set; }

        public DateTime? TokenExpiration { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
