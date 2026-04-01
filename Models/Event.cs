using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string Schedule { get; set; } = string.Empty;

    [StringLength(100)]
    public string Time { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImagePath { get; set; }

    [StringLength(50)]
    public string Icon { get; set; } = "fa-calendar";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}