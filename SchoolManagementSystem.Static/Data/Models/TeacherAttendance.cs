using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class TeacherAttendance
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    [MaxLength(200)]
    public string? Remarks { get; set; }
}
