using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    [Required, MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "Login"
    [Required, MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // "Student", "Teacher", "FeeVoucher"
    [MaxLength(100)]
    public string? EntityId { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
