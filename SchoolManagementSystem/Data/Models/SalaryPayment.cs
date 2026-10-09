using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class SalaryPayment
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary => BasicSalary + Allowances - Deductions;
    public bool IsPaid { get; set; }
    public DateTime? PaidOn { get; set; }
    public DateTime? PaidDate { get => PaidOn; set => PaidOn = value; }
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    [MaxLength(500)]
    public string? Remarks { get; set; }
    public string? PaidBy { get; set; }
}
