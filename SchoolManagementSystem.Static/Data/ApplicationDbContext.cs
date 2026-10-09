using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Settings
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<SchoolSettings> SchoolSettings { get; set; }
        public DbSet<GradeConfig> GradeConfigs { get; set; }

        // Class & Subject
        public DbSet<Class> Classes { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }

        // People
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherDocument> TeacherDocuments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        // Attendance
        public DbSet<StudentAttendance> StudentAttendances { get; set; }
        public DbSet<TeacherAttendance> TeacherAttendances { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }

        // Fee
        public DbSet<FeeHead> FeeHeads { get; set; }
        public DbSet<FeeStructure> FeeStructures { get; set; }
        public DbSet<FeeVoucher> FeeVouchers { get; set; }
        public DbSet<FeeVoucherDetail> FeeVoucherDetails { get; set; }
        public DbSet<FeePayment> FeePayments { get; set; }
        public DbSet<Scholarship> Scholarships { get; set; }

        // Exam
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamSchedule> ExamSchedules { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        // Timetable
        public DbSet<Period> Periods { get; set; }
        public DbSet<TimetableEntry> TimetableEntries { get; set; }

        // Communication
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<SchoolEvent> SchoolEvents { get; set; }

        // Finance
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<SalaryPayment> SalaryPayments { get; set; }

        // Audit
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique constraints
            builder.Entity<StudentAttendance>()
                .HasIndex(a => new { a.StudentId, a.Date }).IsUnique();

            builder.Entity<TeacherAttendance>()
                .HasIndex(a => new { a.TeacherId, a.Date }).IsUnique();

            builder.Entity<TimetableEntry>()
                .HasIndex(t => new { t.SectionId, t.PeriodId, t.DayOfWeek, t.AcademicYearId }).IsUnique();

            builder.Entity<FeeStructure>()
                .HasIndex(f => new { f.ClassId, f.FeeHeadId, f.AcademicYearId }).IsUnique();

            builder.Entity<Student>()
                .HasIndex(s => s.RegistrationNumber).IsUnique();

            // Prevent cascade delete cycles
            builder.Entity<FeeVoucher>()
                .HasOne(v => v.Student)
                .WithMany(s => s.FeeVouchers)
                .HasForeignKey(v => v.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamResult>()
                .HasOne(r => r.Student)
                .WithMany(s => s.ExamResults)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamResult>()
                .HasOne(r => r.Exam)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamResult>()
                .HasOne(r => r.Subject)
                .WithMany()
                .HasForeignKey(r => r.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamSchedule>()
                .HasOne(s => s.Exam)
                .WithMany(e => e.Schedules)
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentAttendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentAttendance>()
                .HasOne(a => a.Section)
                .WithMany()
                .HasForeignKey(a => a.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Section>()
                .HasOne(s => s.ClassTeacher)
                .WithMany(t => t.Sections)
                .HasForeignKey(s => s.ClassTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ClassSubject>()
                .HasOne(cs => cs.Teacher)
                .WithMany(t => t.ClassSubjects)
                .HasForeignKey(cs => cs.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Promotion>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Promotions)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Promotion>()
                .HasOne(p => p.FromClass)
                .WithMany()
                .HasForeignKey(p => p.FromClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Promotion>()
                .HasOne(p => p.ToClass)
                .WithMany()
                .HasForeignKey(p => p.ToClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Promotion>()
                .HasOne(p => p.FromAcademicYear)
                .WithMany()
                .HasForeignKey(p => p.FromAcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Promotion>()
                .HasOne(p => p.ToAcademicYear)
                .WithMany()
                .HasForeignKey(p => p.ToAcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimetableEntry>()
                .HasOne(t => t.Teacher)
                .WithMany()
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimetableEntry>()
                .HasOne(t => t.Section)
                .WithMany(s => s.TimetableEntries)
                .HasForeignKey(t => t.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Expense>()
                .HasOne(e => e.AcademicYear)
                .WithMany()
                .HasForeignKey(e => e.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SalaryPayment>()
                .HasOne(s => s.Teacher)
                .WithMany()
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GradeConfig>()
                .HasOne(g => g.AcademicYear)
                .WithMany()
                .HasForeignKey(g => g.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            // FeeStructure - prevent multiple cascade paths
            builder.Entity<FeeStructure>()
                .HasOne(f => f.Class).WithMany()
                .HasForeignKey(f => f.ClassId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FeeStructure>()
                .HasOne(f => f.FeeHead).WithMany()
                .HasForeignKey(f => f.FeeHeadId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FeeStructure>()
                .HasOne(f => f.AcademicYear).WithMany()
                .HasForeignKey(f => f.AcademicYearId).OnDelete(DeleteBehavior.Restrict);

            // FeeVoucher AcademicYear FK
            builder.Entity<FeeVoucher>()
                .HasOne(v => v.AcademicYear).WithMany()
                .HasForeignKey(v => v.AcademicYearId).OnDelete(DeleteBehavior.Restrict);

            // FeeVoucherDetail FeeHead FK
            builder.Entity<FeeVoucherDetail>()
                .HasOne(d => d.FeeHead).WithMany()
                .HasForeignKey(d => d.FeeHeadId).OnDelete(DeleteBehavior.Restrict);

            // Scholarship StudentId FK
            builder.Entity<Scholarship>()
                .HasOne(s => s.Student).WithMany()
                .HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);

            // ExamSchedule additional FKs
            builder.Entity<ExamSchedule>()
                .HasOne(s => s.Class).WithMany()
                .HasForeignKey(s => s.ClassId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ExamSchedule>()
                .HasOne(s => s.Subject).WithMany()
                .HasForeignKey(s => s.SubjectId).OnDelete(DeleteBehavior.Restrict);

            // TimetableEntry additional FKs
            builder.Entity<TimetableEntry>()
                .HasOne(t => t.Subject).WithMany()
                .HasForeignKey(t => t.SubjectId).OnDelete(DeleteBehavior.Restrict);

            // Receipts: unique number, restrict deletes so money history is never cascaded away
            builder.Entity<FeePayment>()
                .HasIndex(p => p.ReceiptNumber).IsUnique();
            builder.Entity<FeePayment>()
                .HasOne(p => p.FeeVoucher).WithMany(v => v.Payments)
                .HasForeignKey(p => p.FeeVoucherId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FeePayment>()
                .HasIndex(p => p.PaidOn);

            // Decimal precision for money fields
            builder.Entity<FeePayment>()
                .Property(p => p.Amount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeVoucher>()
                .Property(f => f.LateFee).HasColumnType("decimal(10,2)");
            builder.Entity<SchoolSettings>()
                .Property(s => s.LateFeeAmount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeStructure>()
                .Property(f => f.Amount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeVoucher>()
                .Property(f => f.TotalAmount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeVoucher>()
                .Property(f => f.Discount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeVoucher>()
                .Property(f => f.PaidAmount).HasColumnType("decimal(10,2)");
            builder.Entity<FeeVoucherDetail>()
                .Property(f => f.Amount).HasColumnType("decimal(10,2)");
            builder.Entity<Teacher>()
                .Property(t => t.MonthlySalary).HasColumnType("decimal(10,2)");
            builder.Entity<SalaryPayment>()
                .Property(s => s.BasicSalary).HasColumnType("decimal(10,2)");
            builder.Entity<SalaryPayment>()
                .Property(s => s.Allowances).HasColumnType("decimal(10,2)");
            builder.Entity<SalaryPayment>()
                .Property(s => s.Deductions).HasColumnType("decimal(10,2)");
            builder.Entity<Expense>()
                .Property(e => e.Amount).HasColumnType("decimal(10,2)");
            builder.Entity<Scholarship>()
                .Property(s => s.Value).HasColumnType("decimal(10,2)");
            builder.Entity<ExamResult>()
                .Property(r => r.ObtainedMarks).HasColumnType("decimal(6,2)");
            builder.Entity<ExamResult>()
                .Property(r => r.TotalMarks).HasColumnType("decimal(6,2)");
            builder.Entity<GradeConfig>()
                .Property(g => g.MinPercentage).HasColumnType("decimal(5,2)");
            builder.Entity<GradeConfig>()
                .Property(g => g.MaxPercentage).HasColumnType("decimal(5,2)");
        }
    }
}
