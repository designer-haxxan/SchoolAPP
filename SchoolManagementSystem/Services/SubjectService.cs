using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class SubjectService(ApplicationDbContext db)
{
    public async Task<List<Subject>> GetAllAsync()
        => await db.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

    public async Task<List<Subject>> GetAllIncludingInactiveAsync()
        => await db.Subjects.OrderBy(s => s.Name).ToListAsync();

    public async Task<Subject?> GetByIdAsync(int id)
        => await db.Subjects.FindAsync(id);

    public async Task<Subject> CreateAsync(Subject subject)
    {
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        return subject;
    }

    public async Task UpdateAsync(Subject subject)
    {
        db.Subjects.Update(subject);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var s = await db.Subjects.FindAsync(id);
        if (s != null) { s.IsActive = false; await db.SaveChangesAsync(); }
    }

    public async Task<List<ClassSubject>> GetByClassAsync(int classId)
        => await db.ClassSubjects
            .Include(cs => cs.Subject)
            .Include(cs => cs.Teacher)
            .Where(cs => cs.ClassId == classId)
            .ToListAsync();

    public async Task<List<Subject>> GetSubjectsByClassAsync(int classId)
        => await db.ClassSubjects.Include(cs => cs.Subject)
            .Where(cs => cs.ClassId == classId)
            .Select(cs => cs.Subject!)
            .ToListAsync();
}
