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

    /// <summary>DTO transferred over SignalR (no EF tracking).</summary>
    public record ChatMessageDto
    {
        public int Id { get; init; }
        public string SenderName { get; init; } = string.Empty;
        public bool SenderIsAdmin { get; init; }
        public string MessageText { get; init; } = string.Empty;
        public string SentAt { get; init; } = string.Empty;
        public bool IsRead { get; init; }
    }
}
