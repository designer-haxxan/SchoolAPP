using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class AcademicYear
{
    public int Id { get; set; }
    [Required, MaxLength(20)]
    public string Name { get; set; } = string.Empty; // "2024-2025"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Class> Classes { get; set; } = new List<Class>();
}
