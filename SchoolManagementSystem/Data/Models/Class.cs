using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Class
{
    public int Id { get; set; }
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty; // "Nursery", "KG", "Class 1" ... "Class 10"
    public int OrderIndex { get; set; } // For sorting
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
}
