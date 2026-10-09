using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class ExamService(ApplicationDbContext db)
{
    public async Task<List<Exam>> GetAllAsync(int academicYearId)
        => await db.Exams
            .Include(e => e.Schedules).ThenInclude(s => s.Subject)
            .Where(e => e.AcademicYearId == academicYearId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

    public async Task<List<Exam>> GetAllAsync() =>
        await db.Exams.Include(e => e.Schedules).OrderByDescending(e => e.StartDate).ToListAsync();

    public async Task<Exam?> GetByIdAsync(int id)
        => await db.Exams
            .Include(e => e.Schedules).ThenInclude(s => s.Subject)
            .Include(e => e.Schedules).ThenInclude(s => s.Class)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Exam> CreateAsync(Exam exam)
    {
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        return exam;
    }

    public async Task UpdateAsync(Exam exam)
    {
        db.Exams.Update(exam);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var e = await db.Exams.FindAsync(id);
        if (e != null) { db.Exams.Remove(e); await db.SaveChangesAsync(); }
    }

    public async Task<List<ExamResult>> GetResultsAsync(int examId, int classId)
        => await db.ExamResults
            .Include(r => r.Student)
            .Include(r => r.Subject)
            .Where(r => r.ExamId == examId && r.Student.Section != null && r.Student.Section.ClassId == classId)
            .OrderBy(r => r.Student.RollNumber)
            .ToListAsync();

    public async Task<List<ExamResult>> GetStudentResultsAsync(int studentId, int academicYearId)
        => await db.ExamResults
            .Include(r => r.Exam)
            .Include(r => r.Subject)
            .Where(r => r.StudentId == studentId && r.Exam.AcademicYearId == academicYearId)
            .OrderByDescending(r => r.Exam.StartDate)
            .ToListAsync();

    public async Task SaveResultsAsync(List<ExamResult> results, string enteredBy)
    {
        foreach (var result in results)
        {
            var existing = await db.ExamResults
                .FirstOrDefaultAsync(r => r.ExamId == result.ExamId && r.StudentId == result.StudentId && r.SubjectId == result.SubjectId);

            // Assign grade
            var grade = await GetGradeAsync(result.Percentage, result.TotalMarks > 0 ? await GetAcademicYearIdForExamAsync(result.ExamId) : 0);
            result.Grade = grade;
            result.EnteredBy = enteredBy;
            result.EnteredAt = DateTime.UtcNow;

            if (existing != null)
            {
                existing.ObtainedMarks = result.ObtainedMarks;
                existing.IsAbsent = result.IsAbsent;
                existing.Grade = result.Grade;
                existing.EnteredBy = enteredBy;
            }
            else db.ExamResults.Add(result);
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<(Student Student, decimal TotalObtained, decimal TotalMax, decimal Percentage, int Rank)>> GetMeritListAsync(int examId, int classId)
    {
        var results = await db.ExamResults
            .Include(r => r.Student)
            .Where(r => r.ExamId == examId && r.Student.Section != null && r.Student.Section.ClassId == classId && !r.IsAbsent)
            .ToListAsync();

        var studentGroups = results.GroupBy(r => r.StudentId)
            .Select(g => (
                Student: g.First().Student,
                TotalObtained: g.Sum(r => r.ObtainedMarks),
                TotalMax: g.Sum(r => r.TotalMarks),
                Percentage: g.Sum(r => r.TotalMarks) > 0 ? g.Sum(r => r.ObtainedMarks) / g.Sum(r => r.TotalMarks) * 100 : 0m
            ))
            .OrderByDescending(s => s.Percentage)
            .ToList();

        return studentGroups.Select((s, i) => (s.Student, s.TotalObtained, s.TotalMax, Math.Round(s.Percentage, 2), i + 1)).ToList();
    }

    private async Task<string> GetGradeAsync(decimal percentage, int academicYearId)
    {
        if (academicYearId == 0) return "";
        var grade = await db.GradeConfigs
            .Where(g => g.AcademicYearId == academicYearId && percentage >= g.MinPercentage && percentage <= g.MaxPercentage)
            .FirstOrDefaultAsync();
        return grade?.GradeLetter ?? "";
    }

    private async Task<int> GetAcademicYearIdForExamAsync(int examId)
    {
        var exam = await db.Exams.FindAsync(examId);
        return exam?.AcademicYearId ?? 0;
    }

    public async Task<int> GetCurrentAcademicYearIdAsync()
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent);
        return year?.Id ?? 1;
    }

    public async Task<List<ExamResult>> GetResultsByScheduleAsync(int scheduleId)
    {
        var schedule = await db.ExamSchedules.FindAsync(scheduleId);
        if (schedule == null) return [];
        return await db.ExamResults.Where(r => r.ExamId == schedule.ExamId && r.SubjectId == schedule.SubjectId).ToListAsync();
    }

    public async Task<List<ExamResult>> GetResultsByClassAsync(int examId, int classId)
    {
        var subjectIds = await db.ExamSchedules.Where(s => s.ExamId == examId && s.ClassId == classId).Select(s => s.SubjectId).ToListAsync();
        var studentIds = await db.Students.Where(s => s.Section!.ClassId == classId).Select(s => s.Id).ToListAsync();
        return await db.ExamResults
            .Include(r => r.Student)
            .Include(r => r.Subject)
            .Where(r => r.ExamId == examId && subjectIds.Contains(r.SubjectId) && studentIds.Contains(r.StudentId))
            .ToListAsync();
    }

    public async Task<List<ExamResult>> GetResultsByStudentAndExamAsync(int studentId, int examId) =>
        await db.ExamResults.Include(r => r.Subject).Where(r => r.StudentId == studentId && r.ExamId == examId).ToListAsync();

    public async Task<List<ExamResult>> GetLatestStudentResultsAsync(int studentId)
    {
        var latestExam = await db.Exams.OrderByDescending(e => e.EndDate).FirstOrDefaultAsync();
        if (latestExam == null) return [];
        return await GetResultsByStudentAndExamAsync(studentId, latestExam.Id);
    }

    public async Task<List<GradeConfig>> GetGradeConfigsAsync() =>
        await db.GradeConfigs.OrderByDescending(g => g.MinPercentage).ToListAsync();

    // Overload used by MarksEntry page (scheduleId unused but kept for API compatibility)
    public async Task SaveResultsAsync(List<ExamResult> results, int scheduleId)
        => await SaveResultsAsync(results, "system");
}
