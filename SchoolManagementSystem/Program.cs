using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SchoolManagementSystem.Client.Pages;
using SchoolManagementSystem.Components;
using SchoolManagementSystem.Components.Account;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;
using SchoolManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF license (community is free for open source / small teams)
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // Simplified for school app
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("TeacherOrAdmin", p => p.RequireRole("Admin", "Teacher"));
    options.AddPolicy("AccountantOrAdmin", p => p.RequireRole("Admin", "Accountant"));
    options.AddPolicy("StaffOnly", p => p.RequireRole("Admin", "Teacher", "Accountant"));
});

// Register application services
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<TeacherService>();
builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<FeeService>();
builder.Services.AddScoped<ExamService>();
builder.Services.AddScoped<TimetableService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<ReportService>();

// HttpContextAccessor for getting user info in services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SchoolManagementSystem.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    await SeedDataAsync(scope.ServiceProvider);
}

app.Run();

// ============================================================
// DATA SEEDING
// ============================================================
static async Task SeedDataAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    // Apply pending migrations
    await db.Database.MigrateAsync();

    // Seed Roles
    string[] roles = ["Admin", "Teacher", "Accountant", "Parent"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Admin user
    const string adminEmail = "admin@school.pk";
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Seed Academic Year
    if (!db.AcademicYears.Any())
    {
        db.AcademicYears.Add(new AcademicYear
        {
            Name = "2024-2025",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            IsCurrent = true
        });
        await db.SaveChangesAsync();
    }

    // Seed School Settings
    if (!db.SchoolSettings.Any())
    {
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
        await db.SaveChangesAsync();
    }

    // Seed Grade Config
    if (!db.GradeConfigs.Any())
    {
        var year = db.AcademicYears.First(y => y.IsCurrent);
        db.GradeConfigs.AddRange(
            new GradeConfig { GradeLetter = "A+", MinPercentage = 90, MaxPercentage = 100, Remarks = "Outstanding", AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "A",  MinPercentage = 80, MaxPercentage = 89, Remarks = "Excellent",    AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "B+", MinPercentage = 70, MaxPercentage = 79, Remarks = "Very Good",    AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "B",  MinPercentage = 60, MaxPercentage = 69, Remarks = "Good",         AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "C",  MinPercentage = 50, MaxPercentage = 59, Remarks = "Satisfactory", AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "D",  MinPercentage = 40, MaxPercentage = 49, Remarks = "Pass",         AcademicYearId = year.Id },
            new GradeConfig { GradeLetter = "F",  MinPercentage = 0,  MaxPercentage = 39, Remarks = "Fail",         AcademicYearId = year.Id }
        );
        await db.SaveChangesAsync();
    }

    // Seed Fee Heads
    if (!db.FeeHeads.Any())
    {
        db.FeeHeads.AddRange(
            new FeeHead { Name = "Tuition Fee",    IsRecurring = true,  IsActive = true },
            new FeeHead { Name = "Admission Fee",  IsRecurring = false, IsActive = true },
            new FeeHead { Name = "Exam Fee",       IsRecurring = false, IsActive = true },
            new FeeHead { Name = "Library Fee",    IsRecurring = true,  IsActive = true },
            new FeeHead { Name = "Sports Fee",     IsRecurring = true,  IsActive = true },
            new FeeHead { Name = "Transport Fee",  IsRecurring = true,  IsActive = true },
            new FeeHead { Name = "Computer Fee",   IsRecurring = true,  IsActive = true }
        );
        await db.SaveChangesAsync();
    }

    // Seed Periods
    if (!db.Periods.Any())
    {
        db.Periods.AddRange(
            new Period { Name = "Period 1", StartTime = new TimeOnly(8,  0), EndTime = new TimeOnly(8,  45), OrderIndex = 1 },
            new Period { Name = "Period 2", StartTime = new TimeOnly(8,  45), EndTime = new TimeOnly(9, 30), OrderIndex = 2 },
            new Period { Name = "Period 3", StartTime = new TimeOnly(9,  30), EndTime = new TimeOnly(10, 15), OrderIndex = 3 },
            new Period { Name = "Break",    StartTime = new TimeOnly(10, 15), EndTime = new TimeOnly(10, 30), OrderIndex = 4, IsBreak = true },
            new Period { Name = "Period 4", StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(11, 15), OrderIndex = 5 },
            new Period { Name = "Period 5", StartTime = new TimeOnly(11, 15), EndTime = new TimeOnly(12,  0), OrderIndex = 6 },
            new Period { Name = "Period 6", StartTime = new TimeOnly(12,  0), EndTime = new TimeOnly(12, 45), OrderIndex = 7 },
            new Period { Name = "Lunch",    StartTime = new TimeOnly(12, 45), EndTime = new TimeOnly(13, 15), OrderIndex = 8, IsBreak = true },
            new Period { Name = "Period 7", StartTime = new TimeOnly(13, 15), EndTime = new TimeOnly(14,  0), OrderIndex = 9 },
            new Period { Name = "Period 8", StartTime = new TimeOnly(14,  0), EndTime = new TimeOnly(14, 45), OrderIndex = 10 }
        );
        await db.SaveChangesAsync();
    }

    // Seed Classes
    if (!db.Classes.Any())
    {
        var year = db.AcademicYears.First(y => y.IsCurrent);
        string[] classNames = ["Nursery", "KG", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5", "Class 6", "Class 7", "Class 8", "Class 9", "Class 10"];
        for (int i = 0; i < classNames.Length; i++)
        {
            var cls = new Class { Name = classNames[i], OrderIndex = i, AcademicYearId = year.Id };
            db.Classes.Add(cls);
        }
        await db.SaveChangesAsync();

        // Seed default sections for each class
        foreach (var cls in db.Classes.ToList())
        {
            db.Sections.Add(new Section { Name = "A", Capacity = 40, ClassId = cls.Id });
        }
        await db.SaveChangesAsync();
    }

    // Seed Expense Categories
    if (!db.ExpenseCategories.Any())
    {
        db.ExpenseCategories.AddRange(
            new ExpenseCategory { Name = "Utilities",    IsActive = true },
            new ExpenseCategory { Name = "Stationery",   IsActive = true },
            new ExpenseCategory { Name = "Maintenance",  IsActive = true },
            new ExpenseCategory { Name = "Salaries",     IsActive = true },
            new ExpenseCategory { Name = "Events",       IsActive = true },
            new ExpenseCategory { Name = "Miscellaneous",IsActive = true }
        );
        await db.SaveChangesAsync();
    }
}
