using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Announcement
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; }
    public AudienceType Audience { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    [MaxLength(500)]
    public string? AttachmentPath { get; set; }
}
