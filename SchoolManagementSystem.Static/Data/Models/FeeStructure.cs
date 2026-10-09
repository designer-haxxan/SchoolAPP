namespace SchoolManagementSystem.Data.Models;

public class FeeStructure
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int FeeHeadId { get; set; }
    public FeeHead FeeHead { get; set; } = null!;
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public decimal Amount { get; set; }
    public int DueDay { get; set; } = 10; // Day of month fee is due
}
