using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string SenderName { get; set; } = string.Empty;

        public bool SenderIsAdmin { get; set; } = false;

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
