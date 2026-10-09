namespace SchoolManagementSystem.Data.Models;

public class TimetableEntry
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
}
