using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class StudentService(ApplicationDbContext db)
{
    public async Task<(List<Student> Items, int Total)> GetAllAsync(
        int academicYearId,
        string? searchTerm = null,
        int? classId = null,
        int? sectionId = null,
        StudentStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Students
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Class)
            .Include(s => s.Guardian)
            .Where(s => s.AcademicYearId == academicYearId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.RegistrationNumber.ToLower().Contains(term) ||
                (s.FatherName != null && s.FatherName.ToLower().Contains(term)) ||
                (s.BFormNumber != null && s.BFormNumber.ToLower().Contains(term)));
        }

        if (classId.HasValue)
            query = query.Where(s => s.Section != null && s.Section.ClassId == classId.Value);

        if (sectionId.HasValue)
            query = query.Where(s => s.SectionId == sectionId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Section!.Class.OrderIndex)
            .ThenBy(s => s.RollNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Student?> GetByIdAsync(int id)
        => await db.Students
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Class)
            .Include(s => s.Guardian)
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Student> CreateAsync(Student student)
    {
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    public async Task UpdateAsync(Student student)
    {
        db.Students.Update(student);
        if (student.Guardian != null)
        {
            if (student.Guardian.Id == 0) db.Guardians.Add(student.Guardian);
            else db.Guardians.Update(student.Guardian);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var s = await db.Students.FindAsync(id);
        if (s != null) { s.Status = StudentStatus.Inactive; await db.SaveChangesAsync(); }
    }

    public async Task<string> GenerateRegistrationNumberAsync(int academicYearId)
    {
        var year = await db.AcademicYears.FindAsync(academicYearId);
        var yearStr = year?.StartDate.Year.ToString() ?? DateTime.Now.Year.ToString();
        var count = await db.Students.CountAsync(s => s.AcademicYearId == academicYearId) + 1;
        return $"SMS-{yearStr}-{count:D4}";
    }

    public async Task<int> GetNextRollNumberAsync(int sectionId)
    {
        var max = await db.Students
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .MaxAsync(s => (int?)s.RollNumber) ?? 0;
        return max + 1;
    }

    public async Task<List<Student>> GetByClassAsync(int classId)
        => await db.Students
            .Where(s => s.Section != null && s.Section.ClassId == classId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.RollNumber)
            .ToListAsync();

    public async Task<List<Student>> GetBySectionAsync(int sectionId)
        => await db.Students
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.RollNumber)
            .ToListAsync();

    public async Task PromoteStudentsAsync(List<Promotion> promotions)
    {
        foreach (var promo in promotions)
        {
            var student = await db.Students.FindAsync(promo.StudentId);
            if (student == null) continue;
            student.PreviousClassId = student.Section?.ClassId;
            if (promo.Status == PromotionStatus.Promoted)
            {
                student.SectionId = promo.ToSectionId;
                student.AcademicYearId = promo.ToAcademicYearId;
            }
        }
        db.Promotions.AddRange(promotions);
        await db.SaveChangesAsync();
    }

    public async Task<Guardian?> GetGuardianByStudentIdAsync(int studentId)
        => await db.Guardians.FirstOrDefaultAsync(g => g.StudentId == studentId);

    public async Task SaveGuardianAsync(Guardian guardian)
    {
        if (guardian.Id == 0) db.Guardians.Add(guardian);
        else db.Guardians.Update(guardian);
        await db.SaveChangesAsync();
    }
}
