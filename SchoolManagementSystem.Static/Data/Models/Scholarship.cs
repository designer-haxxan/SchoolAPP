using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Scholarship
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty; // "Merit Scholarship"
    public ScholarshipType Type { get; set; }
    public decimal Value { get; set; } // 50 (%) or 500 (PKR)
    public int? FeeHeadId { get; set; }
    public FeeHead? FeeHead { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
