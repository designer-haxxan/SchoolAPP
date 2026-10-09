using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Student
{
    public int Id { get; set; }
    [Required, MaxLength(30)]
    public string RegistrationNumber { get; set; } = string.Empty; // "SMS-2024-0001"
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    [MaxLength(100)]
    public string? FatherName { get; set; }
    public Gender Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    [MaxLength(20)]
    public string? BFormNumber { get; set; }
    [MaxLength(500)]
    public string? PhotoPath { get; set; }
    [MaxLength(50)]
    public string? Religion { get; set; }
    [MaxLength(50)]
    public string Nationality { get; set; } = "Pakistani";
    [MaxLength(500)]
    public string? PermanentAddress { get; set; }
    [MaxLength(500)]
    public string? CurrentAddress { get; set; }
    public DateTime AdmissionDate { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public int? SectionId { get; set; }
    public Section? Section { get; set; }
    public int RollNumber { get; set; }
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public int? PreviousClassId { get; set; }
    public Guardian? Guardian { get; set; }
    public ICollection<FeeVoucher> FeeVouchers { get; set; } = new List<FeeVoucher>();
    public ICollection<StudentAttendance> Attendances { get; set; } = new List<StudentAttendance>();
    public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
