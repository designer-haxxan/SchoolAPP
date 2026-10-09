using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class GradeConfig
{
    public int Id { get; set; }
    [Required, MaxLength(5)]
    public string GradeLetter { get; set; } = string.Empty; // "A+", "A", "B+", "B", "C", "D", "F"
    public decimal MinPercentage { get; set; }
    public decimal MaxPercentage { get; set; }
    [MaxLength(100)]
    public string? Remarks { get; set; } // "Excellent", "Good", "Satisfactory", "Fail"
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
}
