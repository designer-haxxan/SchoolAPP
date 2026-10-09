using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class TimetableService(ApplicationDbContext db)
{
    public async Task<List<Period>> GetPeriodsAsync()
        => await db.Periods.OrderBy(p => p.OrderIndex).ToListAsync();

    public async Task SavePeriodAsync(Period period)
    {
        if (period.Id == 0) db.Periods.Add(period);
        else db.Periods.Update(period);
        await db.SaveChangesAsync();
    }

    public async Task DeletePeriodAsync(int id)
    {
        var p = await db.Periods.FindAsync(id);
        if (p != null) { db.Periods.Remove(p); await db.SaveChangesAsync(); }
    }

    public async Task<List<TimetableEntry>> GetBySectionAsync(int sectionId, int academicYearId)
        => await db.TimetableEntries
            .Include(t => t.Period)
            .Include(t => t.Subject)
            .Include(t => t.Teacher)
            .Where(t => t.SectionId == sectionId && t.AcademicYearId == academicYearId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.Period.OrderIndex)
            .ToListAsync();

    public async Task<List<TimetableEntry>> GetByTeacherAsync(int teacherId, int academicYearId)
        => await db.TimetableEntries
            .Include(t => t.Period)
            .Include(t => t.Subject)
            .Include(t => t.Section).ThenInclude(s => s.Class)
            .Where(t => t.TeacherId == teacherId && t.AcademicYearId == academicYearId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.Period.OrderIndex)
            .ToListAsync();

    public async Task SaveEntryAsync(TimetableEntry entry)
    {
        var existing = await db.TimetableEntries
            .FirstOrDefaultAsync(t => t.SectionId == entry.SectionId && t.PeriodId == entry.PeriodId
                && t.DayOfWeek == entry.DayOfWeek && t.AcademicYearId == entry.AcademicYearId);

        if (existing != null)
        {
            existing.SubjectId = entry.SubjectId;
            existing.TeacherId = entry.TeacherId;
        }
        else db.TimetableEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        var t = await db.TimetableEntries.FindAsync(id);
        if (t != null) { db.TimetableEntries.Remove(t); await db.SaveChangesAsync(); }
    }

    public async Task<List<TimetableEntry>> GetBySectionAsync(int sectionId) =>
        await db.TimetableEntries.Include(t => t.Subject).Include(t => t.Teacher).Include(t => t.Period)
                                 .Where(t => t.SectionId == sectionId).ToListAsync();

    public async Task<List<TimetableEntry>> GetByTeacherAsync(int teacherId) =>
        await db.TimetableEntries.Include(t => t.Subject).Include(t => t.Section).ThenInclude(s => s!.Class).Include(t => t.Period)
                                 .Where(t => t.TeacherId == teacherId).ToListAsync();

    public async Task SaveEntriesAsync(List<TimetableEntry> entries, int sectionId)
    {
        var yearId = (await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent))?.Id ?? 1;
        var existing = await db.TimetableEntries.Where(t => t.SectionId == sectionId && t.AcademicYearId == yearId).ToListAsync();
        db.TimetableEntries.RemoveRange(existing);
        foreach (var e in entries)
        {
            e.SectionId = sectionId; e.AcademicYearId = yearId;
        }
        db.TimetableEntries.AddRange(entries);
        await db.SaveChangesAsync();
    }
}
