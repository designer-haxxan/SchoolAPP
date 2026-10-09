namespace SchoolManagementSystem.Data.Models;

public class ExamSchedule
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TotalMarks { get; set; } = 100;
    public int PassMarks { get; set; } = 40;
}
