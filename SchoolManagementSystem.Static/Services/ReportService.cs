using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class ReportService(ApplicationDbContext db)
{
    public async Task<byte[]> ExportStudentsToExcelAsync(int academicYearId, int? classId = null, int? sectionId = null)
    {
        var query = db.Students
            .Include(s => s.Section).ThenInclude(sec => sec!.Class)
            .Include(s => s.Guardian)
            .Where(s => s.AcademicYearId == academicYearId && s.Status == StudentStatus.Active);

        if (classId.HasValue) query = query.Where(s => s.Section != null && s.Section.ClassId == classId.Value);
        if (sectionId.HasValue) query = query.Where(s => s.SectionId == sectionId.Value);

        var students = await query.OrderBy(s => s.Section!.Class.OrderIndex).ThenBy(s => s.RollNumber).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Students");

        // Header
        var headers = new[] { "#", "Reg No", "Name", "Father Name", "Gender", "DOB", "Class", "Section", "Roll No", "Phone", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Navy;
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var (s, idx) in students.Select((s, i) => (s, i + 1)))
        {
            ws.Cell(row, 1).Value = idx;
            ws.Cell(row, 2).Value = s.RegistrationNumber;
            ws.Cell(row, 3).Value = s.FullName;
            ws.Cell(row, 4).Value = s.FatherName ?? "";
            ws.Cell(row, 5).Value = s.Gender.ToString();
            ws.Cell(row, 6).Value = s.DateOfBirth.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value = s.Section?.Class?.Name ?? "";
            ws.Cell(row, 8).Value = s.Section?.Name ?? "";
            ws.Cell(row, 9).Value = s.RollNumber;
            ws.Cell(row, 10).Value = s.Guardian?.FatherPhone ?? "";
            ws.Cell(row, 11).Value = s.Status.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportFeeReportToExcelAsync(int month, int year, int academicYearId)
    {
        var vouchers = await db.FeeVouchers
            .Include(v => v.Student).ThenInclude(s => s.Section).ThenInclude(sec => sec!.Class)
            .Where(v => v.Month == month && v.Year == year && v.AcademicYearId == academicYearId)
            .OrderBy(v => v.Student.Section!.Class.OrderIndex)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Fee Report");

        var headers = new[] { "#", "Voucher No", "Student", "Class", "Section", "Total", "Discount", "Paid", "Pending", "Status", "Payment Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var (v, idx) in vouchers.Select((v, i) => (v, i + 1)))
        {
            ws.Cell(row, 1).Value = idx;
            ws.Cell(row, 2).Value = v.VoucherNumber;
            ws.Cell(row, 3).Value = v.Student.FullName;
            ws.Cell(row, 4).Value = v.Student.Section?.Class?.Name ?? "";
            ws.Cell(row, 5).Value = v.Student.Section?.Name ?? "";
            ws.Cell(row, 6).Value = v.TotalAmount;
            ws.Cell(row, 7).Value = v.Discount;
            ws.Cell(row, 8).Value = v.PaidAmount;
            ws.Cell(row, 9).Value = v.PendingAmount;
            ws.Cell(row, 10).Value = v.Status.ToString();
            ws.Cell(row, 11).Value = v.PaidOn?.ToString("dd/MM/yyyy") ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAttendanceReportToExcelAsync(int sectionId, int month, int year)
    {
        var section = await db.Sections.Include(s => s.Class).FirstOrDefaultAsync(s => s.Id == sectionId);
        var students = await db.Students
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.RollNumber)
            .ToListAsync();

        var attendance = await db.StudentAttendances
            .Where(a => a.SectionId == sectionId && a.Date.Month == month && a.Date.Year == year)
            .ToListAsync();

        var daysInMonth = DateTime.DaysInMonth(year, month);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance");

        ws.Cell(1, 1).Value = "#";
        ws.Cell(1, 2).Value = "Student Name";
        ws.Cell(1, 3).Value = "Roll No";

        for (int d = 1; d <= daysInMonth; d++)
        {
            ws.Cell(1, d + 3).Value = d.ToString();
            ws.Cell(1, d + 3).Style.Font.Bold = true;
        }

        ws.Cell(1, daysInMonth + 4).Value = "Present";
        ws.Cell(1, daysInMonth + 5).Value = "Absent";
        ws.Cell(1, daysInMonth + 6).Value = "%";

        // Style header row
        var headerRange = ws.Range(1, 1, 1, daysInMonth + 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var (s, idx) in students.Select((s, i) => (s, i + 1)))
        {
            ws.Cell(row, 1).Value = idx;
            ws.Cell(row, 2).Value = s.FullName;
            ws.Cell(row, 3).Value = s.RollNumber;

            int presentCount = 0, absentCount = 0;
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                var rec = attendance.FirstOrDefault(a => a.StudentId == s.Id && a.Date == date);
                if (rec != null)
                {
                    ws.Cell(row, d + 3).Value = rec.Status == AttendanceStatus.Present ? "P" : rec.Status == AttendanceStatus.Absent ? "A" : rec.Status == AttendanceStatus.Late ? "L" : "Leave";
                    if (rec.Status == AttendanceStatus.Present || rec.Status == AttendanceStatus.Late) presentCount++;
                    else absentCount++;
                }
                else ws.Cell(row, d + 3).Value = "-";
            }

            ws.Cell(row, daysInMonth + 4).Value = presentCount;
            ws.Cell(row, daysInMonth + 5).Value = absentCount;
            var total = presentCount + absentCount;
            ws.Cell(row, daysInMonth + 6).Value = total > 0 ? Math.Round((decimal)presentCount / total * 100, 1) + "%" : "-";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
