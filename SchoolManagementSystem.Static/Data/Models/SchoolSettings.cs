using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class SchoolSettings
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string SchoolName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(200)]
    public string? Website { get; set; }
    [MaxLength(500)]
    public string? LogoPath { get; set; }
    [MaxLength(200)]
    public string? PrincipalName { get; set; }
    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }
    [MaxLength(500)]
    public string? SmsGatewayApiKey { get; set; }
    [MaxLength(50)]
    public string? SmsGatewayProvider { get; set; }
    [MaxLength(500)]
    public string? HeaderText { get; set; }
    [MaxLength(500)]
    public string? FooterText { get; set; }
    /// <summary>Default day of month vouchers are due (Pakistani schools usually use the 10th).</summary>
    public int DefaultDueDay { get; set; } = 10;
    /// <summary>Flat late fee charged on unpaid vouchers after the grace period.</summary>
    public decimal LateFeeAmount { get; set; }
    public int LateFeeGraceDays { get; set; }
}
