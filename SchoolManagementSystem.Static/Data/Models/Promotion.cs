namespace SchoolManagementSystem.Data.Models;

public class Promotion
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int FromClassId { get; set; }
    public Class FromClass { get; set; } = null!;
    public int ToClassId { get; set; }
    public Class ToClass { get; set; } = null!;
    public int? ToSectionId { get; set; }
    public Section? ToSection { get; set; }
    public int FromAcademicYearId { get; set; }
    public AcademicYear FromAcademicYear { get; set; } = null!;
    public int ToAcademicYearId { get; set; }
    public AcademicYear ToAcademicYear { get; set; } = null!;
    public DateTime PromotedOn { get; set; } = DateTime.UtcNow;
    public string? PromotedBy { get; set; }
    public PromotionStatus Status { get; set; }
}
