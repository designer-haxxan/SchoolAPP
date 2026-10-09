using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

/// <summary>Billed vs collected for one billing month. Percentage drives the collection-rate ring.</summary>
public record CollectionStats(decimal Billed, decimal Collected)
{
    public decimal Outstanding => Math.Max(0, Billed - Collected);
    public int Percentage => Billed <= 0 ? 0 : (int)Math.Round(Collected / Billed * 100, MidpointRounding.AwayFromZero);
}

public class FeeService(ApplicationDbContext db)
{
    // ───────────────────────── Fee structure ─────────────────────────

    public async Task<List<FeeHead>> GetFeeHeadsAsync() =>
        await db.FeeHeads.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync();

    public async Task<List<FeeStructure>> GetAllStructuresAsync() =>
        await db.FeeStructures.Include(f => f.Class).Include(f => f.FeeHead).ToListAsync();

    public async Task<int> GetCurrentYearIdAsync()
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent);
        return year?.Id ?? 1;
    }

    public async Task SaveStructureAsync(List<FeeStructure> structures, int academicYearId)
    {
        var existing = await db.FeeStructures.Where(f => f.AcademicYearId == academicYearId).ToListAsync();
        db.FeeStructures.RemoveRange(existing);
        db.FeeStructures.AddRange(structures);
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Vouchers ─────────────────────────

    /// <summary>
    /// Builds one month's voucher for a student from the class fee structure, applying
    /// scholarships that are valid for that month. Returns the existing voucher if one exists.
    /// </summary>
    public async Task<FeeVoucher> GenerateVoucherAsync(int studentId, int month, int year, int academicYearId)
    {
        var existing = await db.FeeVouchers
            .FirstOrDefaultAsync(v => v.StudentId == studentId && v.Month == month && v.Year == year);
        if (existing != null) return existing;

        var student = await db.Students.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new InvalidOperationException("Student not found");

        var classId = student.Section?.ClassId ?? 0;
        var settings = await db.SchoolSettings.AsNoTracking().FirstOrDefaultAsync();
        var structures = await db.FeeStructures
            .Include(f => f.FeeHead)
            .Where(f => f.ClassId == classId && f.AcademicYearId == academicYearId)
            .ToListAsync();

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var scholarships = await db.Scholarships
            .Where(s => s.StudentId == studentId && s.IsActive
                && s.EffectiveFrom <= monthEnd
                && (s.EffectiveTo == null || s.EffectiveTo >= monthStart))
            .ToListAsync();

        var dueDay = Math.Clamp(settings?.DefaultDueDay ?? 10, 1, DateTime.DaysInMonth(year, month));
        var voucher = new FeeVoucher
        {
            VoucherNumber = await GenerateVoucherNumberAsync(month, year),
            StudentId = studentId,
            AcademicYearId = academicYearId,
            Month = month,
            Year = year,
            DueDate = new DateTime(year, month, dueDay),
            Status = VoucherStatus.Pending
        };

        foreach (var s in structures)
        {
            voucher.Details.Add(new FeeVoucherDetail { FeeHeadId = s.FeeHeadId, Amount = s.Amount });
        }
        voucher.TotalAmount = voucher.Details.Sum(d => d.Amount);

        // A head-specific scholarship discounts that head's line; a head-less one discounts the total once.
        decimal discount = 0;
        foreach (var sc in scholarships)
        {
            if (sc.FeeHeadId is int headId)
            {
                var line = voucher.Details.FirstOrDefault(d => d.FeeHeadId == headId);
                if (line is null) continue;
                discount += sc.Type == ScholarshipType.Percentage
                    ? line.Amount * sc.Value / 100
                    : Math.Min(sc.Value, line.Amount);
            }
            else
            {
                discount += sc.Type == ScholarshipType.Percentage
                    ? voucher.TotalAmount * sc.Value / 100
                    : sc.Value;
            }
        }
        voucher.Discount = Math.Round(Math.Min(discount, voucher.TotalAmount), 2);
        if (voucher.Discount > 0)
            voucher.DiscountReason = string.Join(", ", scholarships.Select(s => s.Title));

        db.FeeVouchers.Add(voucher);
        await db.SaveChangesAsync();
        return voucher;
    }

    /// <summary>Generates vouchers for every active student (optionally one class) that has a fee structure.</summary>
    public async Task GenerateBulkVouchersAsync(int? classId, int month, int year)
    {
        var yearId = await GetCurrentYearIdAsync();
        var classesWithStructure = await db.FeeStructures
            .Where(f => f.AcademicYearId == yearId)
            .Select(f => f.ClassId).Distinct().ToListAsync();

        var studentsQ = db.Students.Include(s => s.Section)
            .Where(s => s.Status == StudentStatus.Active && s.Section != null
                && classesWithStructure.Contains(s.Section!.ClassId));
        if (classId.HasValue) studentsQ = studentsQ.Where(s => s.Section!.ClassId == classId.Value);

        var studentIds = await studentsQ.Select(s => s.Id).ToListAsync();
        var alreadyBilled = (await db.FeeVouchers
            .Where(v => v.Month == month && v.Year == year && studentIds.Contains(v.StudentId))
            .Select(v => v.StudentId).ToListAsync()).ToHashSet();

        foreach (var id in studentIds.Where(id => !alreadyBilled.Contains(id)))
            await GenerateVoucherAsync(id, month, year, yearId);
    }

    /// <summary>Paged voucher list with filters. Also refreshes overdue state so badges are current.</summary>
    public async Task<(List<FeeVoucher> Items, int Total)> GetVouchersAsync(
        int? month, int year, VoucherStatus? status, string? search, int page, int pageSize)
    {
        await RefreshOverdueAsync();

        var q = db.FeeVouchers
            .Include(v => v.Student).ThenInclude(s => s!.Section).ThenInclude(s => s!.Class)
            .AsQueryable();
        if (month.HasValue) q = q.Where(v => v.Month == month.Value);
        if (year > 0) q = q.Where(v => v.Year == year);
        if (status.HasValue) q = q.Where(v => v.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(v => v.Student!.FirstName.Contains(term) || v.Student.LastName.Contains(term)
                || v.Student.RegistrationNumber.Contains(term) || v.VoucherNumber.Contains(term));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(v => v.Year).ThenByDescending(v => v.Month)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<FeeVoucher?> GetVoucherByIdAsync(int id)
        => await db.FeeVouchers
            .Include(v => v.Student).ThenInclude(s => s!.Section).ThenInclude(sec => sec!.Class)
            .Include(v => v.Details).ThenInclude(d => d.FeeHead)
            .Include(v => v.AcademicYear)
            .Include(v => v.Payments)
            .FirstOrDefaultAsync(v => v.Id == id);

    /// <summary>Every voucher for one student, newest first (student profile page).</summary>
    public async Task<List<FeeVoucher>> GetStudentVouchersAsync(int studentId)
    {
        await RefreshOverdueAsync();
        return await db.FeeVouchers
            .Where(v => v.StudentId == studentId)
            .OrderByDescending(v => v.Year).ThenByDescending(v => v.Month)
            .ToListAsync();
    }

    public async Task<List<FeeVoucher>> GetPendingVouchersAsync(int studentId)
    {
        await RefreshOverdueAsync();
        return await db.FeeVouchers
            .Where(v => v.StudentId == studentId && v.Status != VoucherStatus.Paid)
            .OrderBy(v => v.Year).ThenBy(v => v.Month)
            .ToListAsync();
    }

    public async Task<List<FeeVoucher>> GetPendingReportAsync(int? classId, int? month, int year)
    {
        await RefreshOverdueAsync();
        var q = db.FeeVouchers.Include(v => v.Student).ThenInclude(s => s!.Section).ThenInclude(s => s!.Class)
            .Where(v => v.Status != VoucherStatus.Paid && v.Year == year);
        if (month.HasValue) q = q.Where(v => v.Month == month.Value);
        if (classId.HasValue) q = q.Where(v => v.Student!.Section!.ClassId == classId.Value);
        return await q.OrderBy(v => v.Student!.Section!.Class!.OrderIndex).ThenBy(v => v.Student!.RollNumber).ToListAsync();
    }

    /// <summary>
    /// Marks unpaid vouchers past their due date as Overdue, and charges the school's flat
    /// late fee once the grace period has passed. Idempotent: a voucher is charged only once.
    /// </summary>
    public async Task RefreshOverdueAsync()
    {
        var today = DateTime.Today;
        var settings = await db.SchoolSettings.AsNoTracking().FirstOrDefaultAsync();
        var lateFee = settings?.LateFeeAmount ?? 0m;
        var grace = settings?.LateFeeGraceDays ?? 0;

        var pastDue = await db.FeeVouchers
            .Where(v => v.Status != VoucherStatus.Paid && v.DueDate < today)
            .ToListAsync();

        foreach (var v in pastDue)
        {
            if (lateFee > 0 && v.LateFee == 0 && v.DueDate.AddDays(grace) < today)
                v.LateFee = lateFee;
            v.Status = VoucherStatus.Overdue;
        }
        if (pastDue.Count > 0) await db.SaveChangesAsync();
    }

    // ───────────────────────── Collection ─────────────────────────

    /// <summary>
    /// Records one receipt against a voucher. Partial payments are allowed; the voucher status
    /// and running totals are updated in the same save.
    /// </summary>
    public async Task<FeePayment> CollectPaymentAsync(
        int voucherId, decimal amount, DateTime paidOn, string method, string collectedBy, string? remarks)
    {
        var voucher = await db.FeeVouchers.FirstOrDefaultAsync(v => v.Id == voucherId)
            ?? throw new InvalidOperationException("Voucher not found.");

        amount = Math.Round(amount, 2);
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");
        if (voucher.PendingAmount <= 0)
            throw new InvalidOperationException("This voucher is already fully paid.");
        if (amount > voucher.PendingAmount)
            throw new InvalidOperationException($"Amount exceeds the pending balance of {MoneyFormat.Pkr(voucher.PendingAmount)}.");

        // The receipt number embeds the row id, so insert with a placeholder and assign it right after.
        var payment = new FeePayment
        {
            ReceiptNumber = "TMP-" + Guid.NewGuid().ToString("N"),
            FeeVoucherId = voucher.Id,
            Amount = amount,
            PaidOn = paidOn,
            Method = method,
            CollectedBy = collectedBy,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
        };
        db.FeePayments.Add(payment);

        voucher.PaidAmount += amount;
        voucher.PaymentMethod = method;
        voucher.CollectedBy = collectedBy;
        voucher.PaidOn = paidOn;
        voucher.Status = ComputeStatus(voucher);

        await db.SaveChangesAsync();
        payment.ReceiptNumber = $"RCP-{paidOn:yyyy}-{payment.Id:D6}";
        await db.SaveChangesAsync();
        return payment;
    }

    public async Task<FeePayment?> GetPaymentByIdAsync(int id)
        => await db.FeePayments
            .Include(p => p.FeeVoucher).ThenInclude(v => v.Student).ThenInclude(s => s!.Section).ThenInclude(sec => sec!.Class)
            .Include(p => p.FeeVoucher).ThenInclude(v => v.Details).ThenInclude(d => d.FeeHead)
            .Include(p => p.FeeVoucher).ThenInclude(v => v.AcademicYear)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<FeePayment>> GetRecentPaymentsForStudentAsync(int studentId, int take = 10)
        => await db.FeePayments
            .Include(p => p.FeeVoucher)
            .Where(p => p.FeeVoucher.StudentId == studentId)
            .OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.Id)
            .Take(take).ToListAsync();

    /// <summary>Cash-basis collection for a calendar month: money actually received that month.</summary>
    public async Task<decimal> GetMonthlyCollectionAsync(int month, int year)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await db.FeePayments
            .Where(p => p.PaidOn >= start && p.PaidOn < end)
            .SumAsync(p => p.Amount);
    }

    /// <summary>Accrual view for one billing month: what was billed to students and how much of it is paid.</summary>
    public async Task<CollectionStats> GetCollectionStatsAsync(int month, int year)
    {
        var rows = await db.FeeVouchers
            .Where(v => v.Month == month && v.Year == year)
            .Select(v => new { v.TotalAmount, v.LateFee, v.Discount, v.PaidAmount })
            .ToListAsync();
        var billed = rows.Sum(r => r.TotalAmount + r.LateFee - r.Discount);
        var collected = rows.Sum(r => r.PaidAmount);
        return new CollectionStats(billed, collected);
    }

    /// <summary>Last N months of cash collected, oldest first, for the dashboard chart.</summary>
    public async Task<List<(string Month, decimal Amount)>> GetMonthlyCollectionSummaryAsync(int months = 6)
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
        var payments = await db.FeePayments
            .Where(p => p.PaidOn >= first)
            .Select(p => new { p.PaidOn, p.Amount })
            .ToListAsync();

        return Enumerable.Range(0, months)
            .Select(i => first.AddMonths(i))
            .Select(m => (m.ToString("MMM yy"),
                payments.Where(p => p.PaidOn.Year == m.Year && p.PaidOn.Month == m.Month).Sum(p => p.Amount)))
            .ToList();
    }

    /// <summary>Counts for the fee overview cards, computed in SQL rather than by loading every voucher.</summary>
    public async Task<(int Pending, int Overdue)> GetVoucherCountsAsync(int year)
    {
        var pending = await db.FeeVouchers.CountAsync(v => v.Year == year && v.Status == VoucherStatus.Pending);
        var overdue = await db.FeeVouchers.CountAsync(v => v.Year == year && v.Status == VoucherStatus.Overdue);
        return (pending, overdue);
    }

    // ───────────────────────── Scholarships ─────────────────────────

    public async Task<List<Scholarship>> GetScholarshipsAsync() =>
        await db.Scholarships.Include(s => s.Student).ThenInclude(s => s!.Section).ThenInclude(s => s!.Class).ToListAsync();

    public async Task SaveScholarshipAsync(Scholarship scholarship)
    {
        if (scholarship.Id == 0) db.Scholarships.Add(scholarship);
        else db.Scholarships.Update(scholarship);
        await db.SaveChangesAsync();
    }

    public async Task DeleteScholarshipAsync(int id)
    {
        var s = await db.Scholarships.FindAsync(id);
        if (s != null) { db.Scholarships.Remove(s); await db.SaveChangesAsync(); }
    }

    // ───────────────────────── Exports ─────────────────────────

    public async Task<byte[]> ExportPendingToExcelAsync(int? classId, int? month, int year)
    {
        var vouchers = await GetPendingReportAsync(classId, month, year);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pending Fees");
        string[] headers = ["Student", "Reg No", "Class", "Month", "Total", "Late Fee", "Discount", "Paid", "Pending", "Due Date", "Status"];
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        for (int i = 0; i < vouchers.Count; i++)
        {
            var v = vouchers[i]; int row = i + 2;
            ws.Cell(row, 1).Value = v.Student?.FullName;
            ws.Cell(row, 2).Value = v.Student?.RegistrationNumber;
            ws.Cell(row, 3).Value = v.Student?.Section?.Class?.Name;
            ws.Cell(row, 4).Value = new DateTime(v.Year, v.Month, 1).ToString("MMM yyyy");
            ws.Cell(row, 5).Value = (double)v.TotalAmount;
            ws.Cell(row, 6).Value = (double)v.LateFee;
            ws.Cell(row, 7).Value = (double)v.Discount;
            ws.Cell(row, 8).Value = (double)v.PaidAmount;
            ws.Cell(row, 9).Value = (double)v.PendingAmount;
            ws.Cell(row, 10).Value = v.DueDate.ToString("dd-MMM-yyyy");
            ws.Cell(row, 11).Value = v.Status.ToString();
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ───────────────────────── Helpers ─────────────────────────

    /// <summary>Overdue wins over Partial so an unpaid balance past its due date always shows as Overdue.</summary>
    private static VoucherStatus ComputeStatus(FeeVoucher v)
    {
        if (v.PendingAmount <= 0) return VoucherStatus.Paid;
        if (v.DueDate.Date < DateTime.Today) return VoucherStatus.Overdue;
        return v.PaidAmount > 0 ? VoucherStatus.Partial : VoucherStatus.Pending;
    }

    private async Task<string> GenerateVoucherNumberAsync(int month, int year)
    {
        var count = await db.FeeVouchers.CountAsync(v => v.Month == month && v.Year == year) + 1;
        return $"FEE-{year}-{month:D2}-{count:D5}";
    }
}
