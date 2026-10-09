using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class AttendanceService(ApplicationDbContext db)
{
    /// <summary>Minimum attendance for exam eligibility (common board and school rule in Pakistan).</summary>
    public const int EligibilityThreshold = 75;

    /// <summary>
    /// Attendance percentage used everywhere in the app.
    /// Present and Late count as attended. Holidays and Leave are not counted at all,
    /// so a day off never lowers a student's percentage.
    /// </summary>
    public static decimal ComputePercentage(IEnumerable<StudentAttendance> records)
    {
        var counted = records.Where(r => r.Status is not (AttendanceStatus.Holiday or AttendanceStatus.Leave)).ToList();
        if (counted.Count == 0) return 0;
        var attended = counted.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
        return Math.Round((decimal)attended / counted.Count * 100, 1, MidpointRounding.AwayFromZero);
    }

    public static bool IsEligible(decimal percentage) => percentage >= EligibilityThreshold;

    // ───────────── Students: marking and reading ─────────────

    public async Task<List<Student>> GetStudentsForAttendanceAsync(int sectionId)
        => await db.Students
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.RollNumber)
            .ToListAsync();

    public async Task<List<StudentAttendance>> GetByDateSectionAsync(DateOnly date, int sectionId) =>
        await db.StudentAttendances.Where(a => a.Date == date && a.SectionId == sectionId).ToListAsync();

    /// <summary>
    /// Saves one day's register for a section. Existing rows for the same student and date are
    /// updated, not duplicated, so re-marking a day is safe.
    /// </summary>
    public async Task SaveStudentAttendanceAsync(List<StudentAttendance> records, string? markedBy = null)
    {
        if (records.Count == 0) return;

        var date = records[0].Date;
        var studentIds = records.Select(r => r.StudentId).ToList();

        // Look up by student and date across all sections: the table has a unique (StudentId, Date) index.
        var existing = await db.StudentAttendances
            .Where(a => a.Date == date && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId);

        foreach (var r in records)
        {
            if (existing.TryGetValue(r.StudentId, out var row))
            {
                row.Status = r.Status;
                row.Remarks = r.Remarks;
                row.SectionId = r.SectionId;
                row.MarkedBy = markedBy ?? row.MarkedBy;
            }
            else
            {
                r.MarkedBy = markedBy ?? r.MarkedBy;
                db.StudentAttendances.Add(r);
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<StudentAttendance>> GetMonthlyStudentAttendanceAsync(int studentId, int month, int year)
    {
        var (start, end) = MonthRange(year, month);
        return await db.StudentAttendances
            .Where(a => a.StudentId == studentId && a.Date >= start && a.Date <= end)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetAttendancePercentageAsync(int studentId, int month, int year)
        => ComputePercentage(await GetMonthlyStudentAttendanceAsync(studentId, month, year));

    // ───────────── Dashboard ─────────────

    public async Task<Dictionary<AttendanceStatus, int>> GetTodaySummaryAsync()
    {
        // Group in memory: the in-memory provider used by the static demo can't translate GroupBy.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var statuses = await db.StudentAttendances
            .Where(a => a.Date == today)
            .Select(a => a.Status)
            .ToListAsync();
        return statuses.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<int> GetPresentTodayCountAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await db.StudentAttendances
            .CountAsync(a => a.Date == today && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
    }

    // ───────────── Section summary and export ─────────────

    public async Task<List<AttendanceSummaryRow>> GetSectionSummaryAsync(int month, int year, int? classId)
    {
        var (start, end) = MonthRange(year, month);

        var sectionsQ = db.Sections.Include(s => s.Class).Include(s => s.Students).AsQueryable();
        if (classId.HasValue) sectionsQ = sectionsQ.Where(s => s.ClassId == classId.Value);

        var sections = await sectionsQ.ToListAsync();
        var result = new List<AttendanceSummaryRow>();

        foreach (var section in sections)
        {
            var activeStudents = section.Students.Count(s => s.Status == StudentStatus.Active);
            if (activeStudents == 0) continue;

            var records = await db.StudentAttendances
                .Where(a => a.SectionId == section.Id && a.Date >= start && a.Date <= end)
                .ToListAsync();

            var counted = records.Where(r => r.Status is not (AttendanceStatus.Holiday or AttendanceStatus.Leave)).ToList();
            var workingDays = counted.Select(r => r.Date).Distinct().Count();
            var attended = counted.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
            var absent = counted.Count(r => r.Status == AttendanceStatus.Absent);

            result.Add(new AttendanceSummaryRow
            {
                ClassName = section.Class?.Name ?? "",
                SectionName = section.Name,
                TotalStudents = activeStudents,
                WorkingDays = workingDays,
                AvgPresent = workingDays > 0 ? attended / workingDays : 0,
                AvgAbsent = workingDays > 0 ? absent / workingDays : 0,
                Percentage = (int)Math.Round(ComputePercentage(records), MidpointRounding.AwayFromZero)
            });
        }
        return result;
    }

    public async Task<byte[]> ExportSummaryToExcelAsync(int month, int year)
    {
        var rows = await GetSectionSummaryAsync(month, year, null);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance Summary");
        ws.Cell(1, 1).Value = "Class"; ws.Cell(1, 2).Value = "Section";
        ws.Cell(1, 3).Value = "Total Students"; ws.Cell(1, 4).Value = "Working Days";
        ws.Cell(1, 5).Value = "Avg Present"; ws.Cell(1, 6).Value = "Percentage";
        ws.Cell(1, 7).Value = $"Eligible (>= {EligibilityThreshold}%)";
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; int row = i + 2;
            ws.Cell(row, 1).Value = r.ClassName; ws.Cell(row, 2).Value = r.SectionName;
            ws.Cell(row, 3).Value = r.TotalStudents; ws.Cell(row, 4).Value = r.WorkingDays;
            ws.Cell(row, 5).Value = r.AvgPresent; ws.Cell(row, 6).Value = r.Percentage + "%";
            ws.Cell(row, 7).Value = r.Percentage >= EligibilityThreshold ? "Yes" : "No";
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ───────────── Teachers ─────────────

    public async Task<List<TeacherAttendance>> GetTeacherAttendanceAsync(DateOnly date)
        => await db.TeacherAttendances
            .Include(a => a.Teacher)
            .Where(a => a.Date == date)
            .ToListAsync();

    public async Task<List<TeacherAttendance>> GetTeacherMonthlyAsync(int teacherId, int month, int year)
    {
        var (start, end) = MonthRange(year, month);
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

    // ───────────── Leave applications ─────────────

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

    private static (DateOnly Start, DateOnly End) MonthRange(int year, int month)
        => (new DateOnly(year, month, 1), new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

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
