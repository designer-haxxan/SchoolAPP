using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Expense
{
    public int Id { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    [MaxLength(50)]
    public string? VoucherNumber { get; set; }
    [MaxLength(200)]
    public string? PaidTo { get; set; }
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    [MaxLength(500)]
    public string? ReceiptPath { get; set; }
    public string? EnteredBy { get; set; }
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
