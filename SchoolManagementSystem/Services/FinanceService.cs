using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class FinanceService(ApplicationDbContext db)
{
    public async Task<List<Expense>> GetExpensesAsync(int academicYearId, DateOnly? from = null, DateOnly? to = null, int? categoryId = null)
    {
        var query = db.Expenses
            .Include(e => e.ExpenseCategory)
            .Where(e => e.AcademicYearId == academicYearId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value);
        if (categoryId.HasValue) query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);

        return await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();
    }

    public async Task<Expense> CreateExpenseAsync(Expense expense)
    {
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    public async Task UpdateExpenseAsync(Expense expense)
    {
        db.Expenses.Update(expense);
        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(int id)
    {
        var e = await db.Expenses.FindAsync(id);
        if (e != null) { db.Expenses.Remove(e); await db.SaveChangesAsync(); }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
        => await db.ExpenseCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

    public async Task<List<SalaryPayment>> GetSalaryPaymentsAsync(int month, int year)
    {
        var payments = await db.SalaryPayments
            .Include(s => s.Teacher)
            .Where(s => s.Month == month && s.Year == year)
            .ToListAsync();

        // Auto-generate for teachers without entries
        var teachers = await db.Teachers.Where(t => t.IsActive).ToListAsync();
        var existingIds = payments.Select(p => p.TeacherId).ToHashSet();

        foreach (var teacher in teachers.Where(t => !existingIds.Contains(t.Id)))
        {
            var payment = new SalaryPayment
            {
                TeacherId = teacher.Id,
                Teacher = teacher,
                Month = month,
                Year = year,
                BasicSalary = teacher.MonthlySalary,
                Allowances = 0,
                Deductions = 0,
                IsPaid = false
            };
            payments.Add(payment);
        }

        return payments.OrderBy(p => p.Teacher.FirstName).ToList();
    }

    public async Task SaveSalaryPaymentAsync(SalaryPayment payment)
    {
        if (payment.Id == 0) db.SalaryPayments.Add(payment);
        else db.SalaryPayments.Update(payment);
        await db.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalExpensesAsync(int academicYearId, int? month = null, int? year = null)
    {
        var query = db.Expenses.Where(e => e.AcademicYearId == academicYearId).AsQueryable();
        if (month.HasValue) query = query.Where(e => e.ExpenseDate.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.ExpenseDate.Year == year.Value);
        return await query.SumAsync(e => e.Amount);
    }

    public async Task<List<(DateOnly Date, decimal Income, decimal Expense)>> GetCashBookAsync(int academicYearId, DateOnly from, DateOnly to)
    {
        var fees = await db.FeeVouchers
            .Where(v => v.AcademicYearId == academicYearId && v.PaidOn.HasValue
                && DateOnly.FromDateTime(v.PaidOn.Value) >= from
                && DateOnly.FromDateTime(v.PaidOn.Value) <= to)
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(e => e.AcademicYearId == academicYearId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var result = new List<(DateOnly, decimal, decimal)>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var income = fees.Where(f => f.PaidOn.HasValue && DateOnly.FromDateTime(f.PaidOn.Value) == d).Sum(f => f.PaidAmount);
            var expense = expenses.Where(e => e.ExpenseDate == d).Sum(e => e.Amount);
            if (income > 0 || expense > 0)
                result.Add((d, income, expense));
        }

        return result;
    }

    // --- Additional methods ---

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync() =>
        await db.ExpenseCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

    public async Task<List<Expense>> GetExpensesAsync(int? categoryId, DateTime from, DateTime to)
    {
        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);
        return await db.Expenses.Include(e => e.ExpenseCategory)
                     .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && (categoryId == null || e.ExpenseCategoryId == categoryId))
                     .OrderByDescending(e => e.ExpenseDate).ToListAsync();
    }

    public async Task SaveExpenseAsync(Expense expense)
    {
        if (expense.Id == 0)
        {
            var yearId = (await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent))?.Id ?? 1;
            expense.AcademicYearId = yearId;
            db.Expenses.Add(expense);
        }
        else db.Expenses.Update(expense);
        await db.SaveChangesAsync();
    }

    public async Task MarkSalaryPaidAsync(int teacherId, int month, int year, decimal basicSalary)
    {
        var existing = await db.SalaryPayments.FirstOrDefaultAsync(s => s.TeacherId == teacherId && s.Month == month && s.Year == year);
        if (existing != null)
        {
            existing.IsPaid = true; existing.PaidDate = DateTime.Today;
        }
        else
        {
            db.SalaryPayments.Add(new SalaryPayment
            {
                TeacherId = teacherId, Month = month, Year = year,
                BasicSalary = basicSalary, Allowances = 0, Deductions = 0,
                IsPaid = true, PaidDate = DateTime.Today
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<CashBookEntry>> GetCashBookAsync(DateTime from, DateTime to)
    {
        var entries = new List<CashBookEntry>();

        var payments = await db.FeeVouchers
            .Where(v => v.PaidAmount > 0 && v.Status != VoucherStatus.Pending)
            .ToListAsync();
        entries.AddRange(payments.Select(v => new CashBookEntry
        {
            Date = v.DueDate,
            Description = $"Fee Collection — {new DateTime(v.Year, v.Month, 1):MMM yyyy}",
            Amount = v.PaidAmount, IsIncome = true
        }));

        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);
        var expenses = await db.Expenses.Include(e => e.ExpenseCategory)
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate).ToListAsync();
        entries.AddRange(expenses.Select(e => new CashBookEntry
        {
            Date = e.ExpenseDate.ToDateTime(TimeOnly.MinValue),
            Description = $"{e.ExpenseCategory?.Name} — {e.Description}",
            Amount = e.Amount, IsIncome = false
        }));

        var salaries = await db.SalaryPayments.Include(s => s.Teacher)
            .Where(s => s.IsPaid && s.PaidDate.HasValue && s.PaidDate.Value >= from && s.PaidDate.Value <= to).ToListAsync();
        entries.AddRange(salaries.Select(s => new CashBookEntry
        {
            Date = s.PaidDate!.Value,
            Description = $"Salary — {s.Teacher?.FullName} ({new DateTime(s.Year, s.Month, 1):MMM yyyy})",
            Amount = s.BasicSalary + s.Allowances - s.Deductions, IsIncome = false
        }));

        return entries.Where(e => e.Date >= from && e.Date <= to).OrderBy(e => e.Date).ToList();
    }

    public async Task<byte[]> ExportExpensesToExcelAsync(DateTime from, DateTime to)
    {
        var expenses = await GetExpensesAsync(null, from, to);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Expenses");
        ws.Cell(1, 1).Value = "Date"; ws.Cell(1, 2).Value = "Category";
        ws.Cell(1, 3).Value = "Description"; ws.Cell(1, 4).Value = "Amount";
        for (int i = 0; i < expenses.Count; i++)
        {
            var e = expenses[i]; int row = i + 2;
            ws.Cell(row, 1).Value = e.ExpenseDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = e.ExpenseCategory?.Name;
            ws.Cell(row, 3).Value = e.Description;
            ws.Cell(row, 4).Value = (double)e.Amount;
        }
        ws.Row(1).Style.Font.Bold = true; ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    public async Task<byte[]> ExportSalarySheetAsync(int month, int year)
    {
        var payments = await GetSalaryPaymentsAsync(month, year);
        var teachers = await db.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Salary Sheet");
        ws.Cell(1, 1).Value = "Employee Code"; ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Basic Salary"; ws.Cell(1, 4).Value = "Allowances";
        ws.Cell(1, 5).Value = "Deductions"; ws.Cell(1, 6).Value = "Net Salary"; ws.Cell(1, 7).Value = "Status";
        for (int i = 0; i < teachers.Count; i++)
        {
            var t = teachers[i]; int row = i + 2;
            var pay = payments.FirstOrDefault(p => p.TeacherId == t.Id);
            ws.Cell(row, 1).Value = t.EmployeeCode; ws.Cell(row, 2).Value = t.FullName;
            ws.Cell(row, 3).Value = (double)(pay?.BasicSalary ?? t.MonthlySalary);
            ws.Cell(row, 4).Value = (double)(pay?.Allowances ?? 0);
            ws.Cell(row, 5).Value = (double)(pay?.Deductions ?? 0);
            ws.Cell(row, 6).Value = (double)(pay?.NetSalary ?? t.MonthlySalary);
            ws.Cell(row, 7).Value = pay?.IsPaid == true ? "Paid" : "Unpaid";
        }
        ws.Row(1).Style.Font.Bold = true; ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    public class CashBookEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
    }
}
