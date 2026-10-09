using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Data;

namespace SchoolManagementSystem.Data.Models;

public class Guardian
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Required, MaxLength(100)]
    public string FatherName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? FatherCNIC { get; set; }
    [MaxLength(20)]
    public string? FatherPhone { get; set; }
    [MaxLength(100)]
    public string? FatherOccupation { get; set; }
    [MaxLength(100)]
    public string? MotherName { get; set; }
    [MaxLength(20)]
    public string? MotherCNIC { get; set; }
    [MaxLength(20)]
    public string? MotherPhone { get; set; }
    [MaxLength(100)]
    public string? GuardianName { get; set; }
    [MaxLength(20)]
    public string? GuardianCNIC { get; set; }
    [MaxLength(20)]
    public string? GuardianPhone { get; set; }
    [MaxLength(50)]
    public string? GuardianRelation { get; set; }
    [MaxLength(20)]
    public string? EmergencyContact { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
