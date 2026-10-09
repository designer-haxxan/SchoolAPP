using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class SettingsService(ApplicationDbContext db)
{
    public async Task<SchoolSettings?> GetSchoolSettingsAsync()
        => await db.SchoolSettings.FirstOrDefaultAsync();

    public async Task SaveSchoolSettingsAsync(SchoolSettings settings)
    {
        if (settings.Id == 0)
            db.SchoolSettings.Add(settings);
        else
            db.SchoolSettings.Update(settings);
        await db.SaveChangesAsync();
    }

    public async Task<List<AcademicYear>> GetAcademicYearsAsync()
        => await db.AcademicYears.OrderByDescending(y => y.StartDate).ToListAsync();

    public async Task<AcademicYear?> GetCurrentYearAsync()
        => await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent);

    public async Task<AcademicYear> CreateAcademicYearAsync(AcademicYear year)
    {
        db.AcademicYears.Add(year);
        await db.SaveChangesAsync();
        return year;
    }

    public async Task UpdateAcademicYearAsync(AcademicYear year)
    {
        db.AcademicYears.Update(year);
        await db.SaveChangesAsync();
    }

    public async Task SetCurrentYearAsync(int yearId)
    {
        var years = await db.AcademicYears.ToListAsync();
        foreach (var y in years) y.IsCurrent = (y.Id == yearId);
        await db.SaveChangesAsync();
    }

    public async Task<List<GradeConfig>> GetGradeConfigsAsync(int academicYearId)
        => await db.GradeConfigs
            .Where(g => g.AcademicYearId == academicYearId)
            .OrderByDescending(g => g.MinPercentage)
            .ToListAsync();

    public async Task SaveGradeConfigsAsync(List<GradeConfig> configs)
    {
        var yearId = configs.First().AcademicYearId;
        var existing = await db.GradeConfigs.Where(g => g.AcademicYearId == yearId).ToListAsync();
        db.GradeConfigs.RemoveRange(existing);
        db.GradeConfigs.AddRange(configs);
        await db.SaveChangesAsync();
    }

    public async Task<List<FeeHead>> GetFeeHeadsAsync()
        => await db.FeeHeads.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync();

    public async Task<List<FeeHead>> GetAllFeeHeadsAsync()
        => await db.FeeHeads.OrderBy(f => f.Name).ToListAsync();

    public async Task SaveFeeHeadAsync(FeeHead feeHead)
    {
        if (feeHead.Id == 0) db.FeeHeads.Add(feeHead);
        else db.FeeHeads.Update(feeHead);
        await db.SaveChangesAsync();
    }

    public async Task DeleteFeeHeadAsync(int id)
    {
        var fh = await db.FeeHeads.FindAsync(id);
        if (fh != null) { db.FeeHeads.Remove(fh); await db.SaveChangesAsync(); }
    }
}
