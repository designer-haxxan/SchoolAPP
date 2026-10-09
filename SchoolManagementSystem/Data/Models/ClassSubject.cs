namespace SchoolManagementSystem.Data.Models;

public class ClassSubject
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int TotalMarks { get; set; } = 100;
    public int PassMarks { get; set; } = 40;
}
