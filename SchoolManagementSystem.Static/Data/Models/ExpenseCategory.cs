using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class ExpenseCategory
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Utilities", "Stationery", "Maintenance"
    public bool IsActive { get; set; } = true;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
