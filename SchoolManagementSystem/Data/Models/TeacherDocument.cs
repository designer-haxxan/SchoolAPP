using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class TeacherDocument
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    [Required, MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty; // "CNIC", "Degree", "Experience Letter"
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
