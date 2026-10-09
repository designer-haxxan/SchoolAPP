using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class FeeHead
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Tuition Fee", "Exam Fee"
    [MaxLength(500)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRecurring { get; set; } = true; // Monthly vs one-time
    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
    public ICollection<FeeVoucherDetail> VoucherDetails { get; set; } = new List<FeeVoucherDetail>();
}
