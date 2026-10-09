using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class DashboardStats
{
    public int TotalStudents { get; set; }
    public int PresentToday { get; set; }
    public int TotalTeachers { get; set; }
    public decimal MonthlyCollection { get; set; }
    public int TotalClasses { get; set; }
    public int PendingFees { get; set; }
    public decimal AttendancePercentage { get; set; }
}

public class DashboardService(ApplicationDbContext db, AttendanceService attendanceService, FeeService feeService)
{
    public async Task<DashboardStats> GetStatsAsync(int academicYearId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        var totalStudents = await db.Students
            .CountAsync(s => s.AcademicYearId == academicYearId && s.Status == StudentStatus.Active);

        var totalTeachers = await db.Teachers.CountAsync(t => t.IsActive);

        var presentToday = await attendanceService.GetPresentTodayCountAsync();

        var monthlyCollection = await feeService.GetMonthlyCollectionAsync(now.Month, now.Year);

        var totalClasses = await db.Classes.CountAsync(c => c.AcademicYearId == academicYearId);

        var pendingFees = await db.FeeVouchers
            .CountAsync(v => v.AcademicYearId == academicYearId
                && (v.Status == VoucherStatus.Pending || v.Status == VoucherStatus.Overdue));

        var attendancePct = totalStudents > 0 && presentToday > 0
            ? Math.Round((decimal)presentToday / totalStudents * 100, 1) : 0;

        return new DashboardStats
        {
            TotalStudents = totalStudents,
            PresentToday = presentToday,
            TotalTeachers = totalTeachers,
            MonthlyCollection = monthlyCollection,
            TotalClasses = totalClasses,
            PendingFees = pendingFees,
            AttendancePercentage = attendancePct
        };
    }
}
