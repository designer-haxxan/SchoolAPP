using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class ExamResult
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public decimal ObtainedMarks { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal Percentage => TotalMarks > 0 ? Math.Round((ObtainedMarks / TotalMarks) * 100, 2) : 0;
    public bool IsAbsent { get; set; }
    [MaxLength(5)]
    public string? Grade { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
}
