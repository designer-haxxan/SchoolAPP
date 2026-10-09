using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class AttendanceService(ApplicationDbContext db)
{
    public async Task<List<StudentAttendance>> GetByDateAndSectionAsync(int sectionId, DateOnly date)
        => await db.StudentAttendances
            .Include(a => a.Student)
            .Where(a => a.SectionId == sectionId && a.Date == date)
            .OrderBy(a => a.Student.RollNumber)
            .ToListAsync();

    public async Task<List<Student>> GetStudentsForAttendanceAsync(int sectionId)
        => await db.Students
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.RollNumber)
            .ToListAsync();

    public async Task SaveStudentAttendanceAsync(List<StudentAttendance> attendances, string markedBy)
    {
        var date = attendances.First().Date;
        var sectionId = attendances.First().SectionId;

        var existing = await db.StudentAttendances
            .Where(a => a.SectionId == sectionId && a.Date == date)
            .ToListAsync();

        db.StudentAttendances.RemoveRange(existing);

        foreach (var a in attendances) a.MarkedBy = markedBy;
        db.StudentAttendances.AddRange(attendances);
        await db.SaveChangesAsync();
    }

    public async Task<List<TeacherAttendance>> GetTeacherAttendanceAsync(DateOnly date)
        => await db.TeacherAttendances
            .Include(a => a.Teacher)
            .Where(a => a.Date == date)
            .ToListAsync();

    public async Task<Dictionary<AttendanceStatus, int>> GetTodaySummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Group in memory: the in-memory provider used by the static demo can't translate GroupBy.
        var statuses = await db.StudentAttendances
            .Where(a => a.Date == today)
            .Select(a => a.Status)
            .ToListAsync();
        return statuses
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<StudentAttendance>> GetMonthlyStudentAttendanceAsync(int studentId, int month, int year)
        => await db.StudentAttendances
            .Where(a => a.StudentId == studentId && a.Date.Month == month && a.Date.Year == year)
            .OrderBy(a => a.Date)
            .ToListAsync();

    public async Task<decimal> GetAttendancePercentageAsync(int studentId, int month, int year)
    {
        var records = await db.StudentAttendances
            .Where(a => a.StudentId == studentId && a.Date.Month == month && a.Date.Year == year)
            .ToListAsync();
        if (!records.Any()) return 0;
        var present = records.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
        return Math.Round((decimal)present / records.Count * 100, 1);
    }

    public async Task<Dictionary<int, decimal>> GetSectionAttendancePercentageAsync(int sectionId, int month, int year)
    {
        var records = await db.StudentAttendances
            .Where(a => a.SectionId == sectionId && a.Date.Month == month && a.Date.Year == year)
            .ToListAsync();

        return records.GroupBy(r => r.StudentId).ToDictionary(
            g => g.Key,
            g =>
            {
                var total = g.Count();
                var present = g.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
                return total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0m;
            });
    }

    public async Task<int> GetPresentTodayCountAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await db.StudentAttendances
            .CountAsync(a => a.Date == today && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
    }

    // Leave applications
    public async Task<List<LeaveApplication>> GetLeaveApplicationsAsync(bool pendingOnly = false)
    {
        var query = db.LeaveApplications
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .AsQueryable();
        if (pendingOnly) query = query.Where(l => l.Status == LeaveStatus.Pending);
        return await query.OrderByDescending(l => l.AppliedOn).ToListAsync();
    }

    public async Task ApproveLeaveAsync(int id, string approvedBy)
    {
        var leave = await db.LeaveApplications.FindAsync(id);
        if (leave != null)
        {
            leave.Status = LeaveStatus.Approved;
            leave.ApprovedBy = approvedBy;
            await db.SaveChangesAsync();
        }
    }

    public async Task RejectLeaveAsync(int id)
    {
        var leave = await db.LeaveApplications.FindAsync(id);
        if (leave != null) { leave.Status = LeaveStatus.Rejected; await db.SaveChangesAsync(); }
    }

    // --- Additional methods ---

    public async Task<List<StudentAttendance>> GetByDateSectionAsync(DateOnly date, int sectionId) =>
        await db.StudentAttendances.Where(a => a.Date == date && a.SectionId == sectionId).ToListAsync();

    public async Task SaveStudentAttendanceAsync(List<StudentAttendance> records)
    {
        foreach (var r in records)
        {
            var existing = await db.StudentAttendances.FirstOrDefaultAsync(a => a.StudentId == r.StudentId && a.Date == r.Date);
            if (existing != null)
            {
                existing.Status = r.Status; existing.Remarks = r.Remarks;
                existing.SectionId = r.SectionId;
            }
            else db.StudentAttendances.Add(r);
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<TeacherAttendance>> GetTeacherMonthlyAsync(int teacherId, int month, int year)
    {
        var start = new DateOnly(year, month, 1);
        var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return await db.TeacherAttendances.Where(a => a.TeacherId == teacherId && a.Date >= start && a.Date <= end).ToListAsync();
    }

    public async Task<List<TeacherAttendance>> GetTeacherByDateAsync(DateOnly date) =>
        await db.TeacherAttendances.Where(a => a.Date == date).ToListAsync();

    public async Task SaveTeacherAttendanceAsync(List<TeacherAttendance> records)
    {
        foreach (var r in records)
        {
            var existing = await db.TeacherAttendances.FirstOrDefaultAsync(a => a.TeacherId == r.TeacherId && a.Date == r.Date);
            if (existing != null)
            {
                existing.Status = r.Status; existing.CheckIn = r.CheckIn; existing.CheckOut = r.CheckOut;
            }
            else db.TeacherAttendances.Add(r);
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<AttendanceSummaryRow>> GetSectionSummaryAsync(int month, int year, int? classId)
    {
        var start = new DateOnly(year, month, 1);
        var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var sectionsQ = db.Sections.Include(s => s.Class).Include(s => s.Students).AsQueryable();
        if (classId.HasValue) sectionsQ = sectionsQ.Where(s => s.ClassId == classId.Value);

        var sections = await sectionsQ.ToListAsync();
        var result = new List<AttendanceSummaryRow>();

        foreach (var section in sections)
        {
            var studentIds = section.Students?.Select(s => s.Id).ToList() ?? [];
            if (studentIds.Count == 0) continue;

            var records = await db.StudentAttendances
                .Where(a => a.SectionId == section.Id && a.Date >= start && a.Date <= end)
                .ToListAsync();

            var workingDays = records.Select(r => r.Date).Distinct().Count();
            var presentCount = records.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
            var absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
            var totalExpected = studentIds.Count * workingDays;
            var pct = totalExpected > 0 ? (int)(presentCount * 100.0 / totalExpected) : 0;

            result.Add(new AttendanceSummaryRow
            {
                ClassName = section.Class?.Name ?? "",
                SectionName = section.Name,
                TotalStudents = studentIds.Count,
                WorkingDays = workingDays,
                AvgPresent = workingDays > 0 ? presentCount / Math.Max(workingDays, 1) : 0,
                AvgAbsent = workingDays > 0 ? absentCount / Math.Max(workingDays, 1) : 0,
                Percentage = pct
            });
        }
        return result;
    }

    public async Task<decimal> GetStudentAttendancePercentAsync(int studentId, int month, int year)
    {
        var start = new DateOnly(year, month, 1);
        var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var records = await db.StudentAttendances.Where(a => a.StudentId == studentId && a.Date >= start && a.Date <= end).ToListAsync();
        if (records.Count == 0) return 0;
        var present = records.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
        return (decimal)present / records.Count * 100;
    }

    public async Task<byte[]> ExportSummaryToExcelAsync(int month, int year)
    {
        var rows = await GetSectionSummaryAsync(month, year, null);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance Summary");
        ws.Cell(1, 1).Value = "Class"; ws.Cell(1, 2).Value = "Section";
        ws.Cell(1, 3).Value = "Total Students"; ws.Cell(1, 4).Value = "Working Days";
        ws.Cell(1, 5).Value = "Avg Present"; ws.Cell(1, 6).Value = "Percentage";
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; int row = i + 2;
            ws.Cell(row, 1).Value = r.ClassName; ws.Cell(row, 2).Value = r.SectionName;
            ws.Cell(row, 3).Value = r.TotalStudents; ws.Cell(row, 4).Value = r.WorkingDays;
            ws.Cell(row, 5).Value = r.AvgPresent; ws.Cell(row, 6).Value = r.Percentage + "%";
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public class AttendanceSummaryRow
    {
        public string ClassName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public int TotalStudents { get; set; }
        public int WorkingDays { get; set; }
        public int AvgPresent { get; set; }
        public int AvgAbsent { get; set; }
        public int Percentage { get; set; }
    }
}
