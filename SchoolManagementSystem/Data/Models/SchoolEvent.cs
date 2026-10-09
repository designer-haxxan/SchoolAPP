using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class SchoolEvent
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    [MaxLength(200)]
    public string? Location { get; set; }
    public EventType Type { get; set; }
    public bool IsHoliday { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
