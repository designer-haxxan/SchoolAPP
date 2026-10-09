using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Data;

/// <summary>
/// Demo data for the GitHub Pages build. Mirrors the server app's first-run seed
/// (settings, classes, fee heads, periods) and adds a few students so screens aren't empty.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.AcademicYears.AnyAsync()) return;

        var year = new AcademicYear
        {
            Name = "2026-2027",
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2027, 3, 31),
            IsCurrent = true
        };
        db.AcademicYears.Add(year);

        db.SchoolSettings.Add(new SchoolSettings
        {
            SchoolName = "Al-Falah Public School",
            Address = "123 Main Road, Model Town",
            City = "Lahore",
            Phone = "042-1234567",
            Email = "info@alfalahschool.pk",
            PrincipalName = "Muhammad Ali",
            RegistrationNumber = "LHR-2010-0001",
            DefaultDueDay = 10,
            LateFeeAmount = 100,
            LateFeeGraceDays = 0
        });

        db.GradeConfigs.AddRange(
            new GradeConfig { GradeLetter = "A+", MinPercentage = 90, MaxPercentage = 100, Remarks = "Outstanding", AcademicYear = year },
            new GradeConfig { GradeLetter = "A",  MinPercentage = 80, MaxPercentage = 89, Remarks = "Excellent", AcademicYear = year },
            new GradeConfig { GradeLetter = "B+", MinPercentage = 70, MaxPercentage = 79, Remarks = "Very Good", AcademicYear = year },
            new GradeConfig { GradeLetter = "B",  MinPercentage = 60, MaxPercentage = 69, Remarks = "Good", AcademicYear = year },
            new GradeConfig { GradeLetter = "C",  MinPercentage = 50, MaxPercentage = 59, Remarks = "Satisfactory", AcademicYear = year },
            new GradeConfig { GradeLetter = "D",  MinPercentage = 40, MaxPercentage = 49, Remarks = "Pass", AcademicYear = year },
            new GradeConfig { GradeLetter = "F",  MinPercentage = 0,  MaxPercentage = 39, Remarks = "Fail", AcademicYear = year });

        db.FeeHeads.AddRange(
            new FeeHead { Name = "Tuition Fee", IsRecurring = true, IsActive = true },
            new FeeHead { Name = "Admission Fee", IsRecurring = false, IsActive = true },
            new FeeHead { Name = "Exam Fee", IsRecurring = false, IsActive = true },
            new FeeHead { Name = "Library Fee", IsRecurring = true, IsActive = true },
            new FeeHead { Name = "Sports Fee", IsRecurring = true, IsActive = true },
            new FeeHead { Name = "Transport Fee", IsRecurring = true, IsActive = true },
            new FeeHead { Name = "Computer Fee", IsRecurring = true, IsActive = true });

        db.Periods.AddRange(
            new Period { Name = "Period 1", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(8, 45), OrderIndex = 1 },
            new Period { Name = "Period 2", StartTime = new TimeOnly(8, 45), EndTime = new TimeOnly(9, 30), OrderIndex = 2 },
            new Period { Name = "Period 3", StartTime = new TimeOnly(9, 30), EndTime = new TimeOnly(10, 15), OrderIndex = 3 },
            new Period { Name = "Break", StartTime = new TimeOnly(10, 15), EndTime = new TimeOnly(10, 30), OrderIndex = 4, IsBreak = true },
            new Period { Name = "Period 4", StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(11, 15), OrderIndex = 5 },
            new Period { Name = "Period 5", StartTime = new TimeOnly(11, 15), EndTime = new TimeOnly(12, 0), OrderIndex = 6 },
            new Period { Name = "Period 6", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(12, 45), OrderIndex = 7 },
            new Period { Name = "Lunch", StartTime = new TimeOnly(12, 45), EndTime = new TimeOnly(13, 15), OrderIndex = 8, IsBreak = true },
            new Period { Name = "Period 7", StartTime = new TimeOnly(13, 15), EndTime = new TimeOnly(14, 0), OrderIndex = 9 },
            new Period { Name = "Period 8", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(14, 45), OrderIndex = 10 });

        db.ExpenseCategories.AddRange(
            new ExpenseCategory { Name = "Utilities", IsActive = true },
            new ExpenseCategory { Name = "Stationery", IsActive = true },
            new ExpenseCategory { Name = "Maintenance", IsActive = true },
            new ExpenseCategory { Name = "Salaries", IsActive = true },
            new ExpenseCategory { Name = "Events", IsActive = true },
            new ExpenseCategory { Name = "Miscellaneous", IsActive = true });

        string[] classNames = ["Nursery", "KG", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5", "Class 6", "Class 7", "Class 8", "Class 9", "Class 10"];
        for (int i = 0; i < classNames.Length; i++)
        {
            var cls = new Class { Name = classNames[i], OrderIndex = i, AcademicYear = year };
            db.Classes.Add(cls);
            db.Sections.Add(new Section { Name = "A", Capacity = 40, Class = cls });
        }
        await db.SaveChangesAsync();

        var tuition = await db.FeeHeads.FirstAsync(f => f.Name == "Tuition Fee");
        var library = await db.FeeHeads.FirstAsync(f => f.Name == "Library Fee");
        var classes = await db.Classes.OrderBy(c => c.OrderIndex).ToListAsync();
        foreach (var cls in classes)
        {
            db.FeeStructures.Add(new FeeStructure { ClassId = cls.Id, FeeHeadId = tuition.Id, AcademicYearId = year.Id, Amount = 3000 });
            db.FeeStructures.Add(new FeeStructure { ClassId = cls.Id, FeeHeadId = library.Id, AcademicYearId = year.Id, Amount = 300 });
        }

        var class1Section = await db.Sections.FirstAsync(s => s.Class!.Name == "Class 1");
        var class3Section = await db.Sections.FirstAsync(s => s.Class!.Name == "Class 3");
        var students = new[]
        {
            ("Ali", "Raza", "Imran Raza", class1Section.Id, 1),
            ("Hira", "Khan", "Tariq Khan", class1Section.Id, 2),
            ("Hamza", "Malik", "Adnan Malik", class1Section.Id, 3),
            ("Ayesha", "Siddiqui", "Farhan Siddiqui", class3Section.Id, 1),
            ("Usman", "Sheikh", "Naveed Sheikh", class3Section.Id, 2),
        };
        for (int i = 0; i < students.Length; i++)
        {
            var (first, last, father, sectionId, roll) = students[i];
            db.Students.Add(new Student
            {
                RegistrationNumber = $"SMS-2026-{i + 1:D4}",
                FirstName = first,
                LastName = last,
                FatherName = father,
                Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                DateOfBirth = new DateTime(2017, 1, 1).AddMonths(i * 4),
                AdmissionDate = new DateTime(2026, 4, 1),
                Status = StudentStatus.Active,
                SectionId = sectionId,
                RollNumber = roll,
                AcademicYear = year
            });
        }
        await db.SaveChangesAsync();
    }
}
