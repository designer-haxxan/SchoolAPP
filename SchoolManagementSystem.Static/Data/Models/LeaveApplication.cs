using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class LeaveApplication
{
    public int Id { get; set; }
    public LeaveApplicantType ApplicantType { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? ApprovedBy { get; set; }
    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    [MaxLength(500)]
    public string? Remarks { get; set; }
}
