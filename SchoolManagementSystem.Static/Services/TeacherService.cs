using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class TeacherService(ApplicationDbContext db)
{
    public async Task<List<Teacher>> GetAllAsync(bool? isActive = null)
    {
        var query = db.Teachers.Include(t => t.ClassSubjects).ThenInclude(cs => cs.Class).AsQueryable();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        return await query.OrderBy(t => t.FirstName).ToListAsync();
    }

    public async Task<(List<Teacher> Items, int Total)> GetAllAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var q = db.Teachers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.FirstName.Contains(search) || t.LastName.Contains(search) || t.CNIC.Contains(search) || t.EmployeeCode.Contains(search));
        if (isActive.HasValue)
            q = q.Where(t => t.IsActive == isActive.Value);
        var total = await q.CountAsync();
        var items = await q.OrderBy(t => t.FirstName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Teacher?> GetByIdAsync(int id)
        => await db.Teachers
            .Include(t => t.Documents)
            .Include(t => t.ClassSubjects).ThenInclude(cs => cs.Class)
            .Include(t => t.ClassSubjects).ThenInclude(cs => cs.Subject)
            .Include(t => t.Sections).ThenInclude(s => s.Class)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Teacher> CreateAsync(Teacher teacher)
    {
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        return teacher;
    }

    public async Task CreateAsync(Teacher teacher, string? initialPassword)
    {
        teacher.EmployeeCode = await GenerateEmployeeCodeAsync();
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Teacher teacher)
    {
        db.Teachers.Update(teacher);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var t = await db.Teachers.FindAsync(id);
        if (t != null) { t.IsActive = false; await db.SaveChangesAsync(); }
    }

    public async Task<string> GenerateEmployeeCodeAsync()
    {
        var count = await db.Teachers.CountAsync() + 1;
        return $"TCH-{count:D4}";
    }

    public async Task AddDocumentAsync(TeacherDocument doc)
    {
        db.TeacherDocuments.Add(doc);
        await db.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int id)
    {
        var doc = await db.TeacherDocuments.FindAsync(id);
        if (doc != null) { db.TeacherDocuments.Remove(doc); await db.SaveChangesAsync(); }
    }

    public async Task<List<Teacher>> GetBySubjectAsync(int subjectId) =>
        await db.Teachers.Where(t => t.ClassSubjects!.Any(cs => cs.SubjectId == subjectId) && t.IsActive).ToListAsync();

    public async Task<byte[]> ExportToExcelAsync()
    {
        var teachers = await db.Teachers.OrderBy(t => t.FirstName).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Teachers");
        ws.Cell(1, 1).Value = "Employee Code"; ws.Cell(1, 2).Value = "Full Name";
        ws.Cell(1, 3).Value = "CNIC"; ws.Cell(1, 4).Value = "Phone";
        ws.Cell(1, 5).Value = "Qualification"; ws.Cell(1, 6).Value = "Joining Date";
        ws.Cell(1, 7).Value = "Monthly Salary"; ws.Cell(1, 8).Value = "Status";
        for (int i = 0; i < teachers.Count; i++)
        {
            var t = teachers[i]; int row = i + 2;
            ws.Cell(row, 1).Value = t.EmployeeCode; ws.Cell(row, 2).Value = t.FullName;
            ws.Cell(row, 3).Value = t.CNIC; ws.Cell(row, 4).Value = t.Phone;
            ws.Cell(row, 5).Value = t.Qualification; ws.Cell(row, 6).Value = t.JoiningDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value = (double)t.MonthlySalary; ws.Cell(row, 8).Value = t.IsActive ? "Active" : "Inactive";
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
