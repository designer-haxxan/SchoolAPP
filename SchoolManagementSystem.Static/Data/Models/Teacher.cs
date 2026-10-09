using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Data;

namespace SchoolManagementSystem.Data.Models;

public class Teacher
{
    public int Id { get; set; }
    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty; // "TCH-0001"
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    [MaxLength(100)]
    public string? FatherName { get; set; }
    public Gender Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    [Required, MaxLength(20)]
    public string CNIC { get; set; } = string.Empty; // "XXXXX-XXXXXXX-X"
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(500)]
    public string? PhotoPath { get; set; }
    [Required, MaxLength(200)]
    public string Qualification { get; set; } = string.Empty; // "B.Ed", "M.A"
    [MaxLength(200)]
    public string? Specialization { get; set; }
    public int ExperienceYears { get; set; }
    public DateTime JoiningDate { get; set; }
    public decimal MonthlySalary { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(100)]
    public string? BankAccount { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<TeacherDocument> Documents { get; set; } = new List<TeacherDocument>();
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
