using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class ClassService(ApplicationDbContext db)
{
    public async Task<List<Class>> GetAllAsync(int academicYearId)
        => await db.Classes
            .Include(c => c.Sections)
                .ThenInclude(s => s.ClassTeacher)
            .Where(c => c.AcademicYearId == academicYearId)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

    public async Task<List<Class>> GetAllAsync()
    {
        var yearId = (await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent))?.Id ?? 1;
        return await GetAllAsync(yearId);
    }

    public async Task<Class?> GetByIdAsync(int id)
        => await db.Classes
            .Include(c => c.Sections)
                .ThenInclude(s => s.ClassTeacher)
            .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Teacher)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Class> CreateAsync(Class cls)
    {
        db.Classes.Add(cls);
        await db.SaveChangesAsync();
        return cls;
    }

    public async Task UpdateAsync(Class cls)
    {
        db.Classes.Update(cls);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cls = await db.Classes.FindAsync(id);
        if (cls != null) { db.Classes.Remove(cls); await db.SaveChangesAsync(); }
    }

    public async Task<Section> CreateSectionAsync(Section section)
    {
        db.Sections.Add(section);
        await db.SaveChangesAsync();
        return section;
    }

    public async Task UpdateSectionAsync(Section section)
    {
        db.Sections.Update(section);
        await db.SaveChangesAsync();
    }

    public async Task DeleteSectionAsync(int id)
    {
        var sec = await db.Sections.FindAsync(id);
        if (sec != null) { db.Sections.Remove(sec); await db.SaveChangesAsync(); }
    }

    public async Task<List<Section>> GetSectionsAsync(int classId)
        => await db.Sections
            .Include(s => s.ClassTeacher)
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<List<Section>> GetAllSectionsAsync(int academicYearId)
        => await db.Sections
            .Include(s => s.Class)
            .Where(s => s.Class.AcademicYearId == academicYearId)
            .OrderBy(s => s.Class.OrderIndex).ThenBy(s => s.Name)
            .ToListAsync();

    public async Task AssignClassSubjectAsync(ClassSubject cs)
    {
        var existing = await db.ClassSubjects
            .FirstOrDefaultAsync(x => x.ClassId == cs.ClassId && x.SubjectId == cs.SubjectId);
        if (existing != null)
        {
            existing.TeacherId = cs.TeacherId;
            existing.TotalMarks = cs.TotalMarks;
            existing.PassMarks = cs.PassMarks;
        }
        else db.ClassSubjects.Add(cs);
        await db.SaveChangesAsync();
    }

    public async Task RemoveClassSubjectAsync(int id)
    {
        var cs = await db.ClassSubjects.FindAsync(id);
        if (cs != null) { db.ClassSubjects.Remove(cs); await db.SaveChangesAsync(); }
    }
}
