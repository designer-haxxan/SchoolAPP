using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

/// <summary>
/// One receipt-level payment against a voucher. A voucher can be settled in
/// several instalments (common in Pakistani schools), so every collection is
/// recorded here and the voucher only keeps a running total.
/// </summary>
public class FeePayment
{
    public int Id { get; set; }
    [Required, MaxLength(40)]
    public string ReceiptNumber { get; set; } = string.Empty; // "RCP-2026-000123"
    public int FeeVoucherId { get; set; }
    public FeeVoucher FeeVoucher { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; }
    [Required, MaxLength(50)]
    public string Method { get; set; } = PaymentMethods.Cash;
    [MaxLength(100)]
    public string? CollectedBy { get; set; }
    [MaxLength(300)]
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Payment rails used by Pakistani schools. Stored as text so new rails need no migration.</summary>
public static class PaymentMethods
{
    public const string Cash = "Cash";
    public static readonly string[] All =
    [
        Cash,
        "Bank Challan (1-Link)",
        "JazzCash",
        "Easypaisa",
        "Raast",
        "Bank Transfer",
        "Cheque",
        "Online Card"
    ];
}
