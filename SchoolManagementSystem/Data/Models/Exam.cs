using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Exam
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "1st Monthly", "Mid-Term", "Final"
    public ExamType Type { get; set; }
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsResultDeclared { get; set; }
    public ICollection<ExamSchedule> Schedules { get; set; } = new List<ExamSchedule>();
    public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
}
