using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Section
{
    public int Id { get; set; }
    [Required, MaxLength(10)]
    public string Name { get; set; } = string.Empty; // "A", "B", "C"
    public int Capacity { get; set; } = 40;
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int? ClassTeacherId { get; set; }
    public Teacher? ClassTeacher { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
}
