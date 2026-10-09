using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<SchoolManagementSystem.Components.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Demo build: all data lives in the browser tab and resets on reload.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("SchoolDemo"));

// Demo sign-in replaces ASP.NET Core Identity (which needs a server).
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("TeacherOrAdmin", p => p.RequireRole("Admin", "Teacher"));
    options.AddPolicy("AccountantOrAdmin", p => p.RequireRole("Admin", "Accountant"));
    options.AddPolicy("StaffOnly", p => p.RequireRole("Admin", "Teacher", "Accountant"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<DemoAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<DemoAuthStateProvider>());

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

var host = builder.Build();

var db = host.Services.GetRequiredService<ApplicationDbContext>();
await DemoDataSeeder.SeedAsync(db);

await host.RunAsync();
