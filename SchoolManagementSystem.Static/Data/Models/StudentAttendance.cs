using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class StudentAttendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    [MaxLength(200)]
    public string? Remarks { get; set; }
    public string? MarkedBy { get; set; }
}
