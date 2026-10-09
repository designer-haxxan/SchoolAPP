using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class FeeVoucher
{
    public int Id { get; set; }
    [Required, MaxLength(30)]
    public string VoucherNumber { get; set; } = string.Empty; // "FEE-2024-10-00001"
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    /// <summary>Flat late fee added once the voucher passes its due date (plus grace days).</summary>
    public decimal LateFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount => TotalAmount + LateFee - Discount - PaidAmount;
    public VoucherStatus Status { get; set; } = VoucherStatus.Pending;
    public DateTime? PaidOn { get; set; }
    [MaxLength(50)]
    public string? PaymentMethod { get; set; } // "Cash", "Bank", "Online"
    public string? CollectedBy { get; set; }
    [MaxLength(200)]
    public string? DiscountReason { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)]
    public string? Remarks { get; set; }
    public ICollection<FeeVoucherDetail> Details { get; set; } = new List<FeeVoucherDetail>();
    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();
}
