using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Subject
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Mathematics"
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty; // "MATH-01"
    public SubjectType Type { get; set; } = SubjectType.Theory;
    public int TotalMarks { get; set; } = 100;
    public int PassMarks { get; set; } = 40;
    public bool IsActive { get; set; } = true;
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}
