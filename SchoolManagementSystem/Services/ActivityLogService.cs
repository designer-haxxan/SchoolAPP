using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class ActivityLogService(ApplicationDbContext db)
{
    public async Task LogAsync(string userName, string action, string entityType, string? entityId = null, string? description = null, string? userId = null)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ActivityLog>> GetRecentAsync(int count = 20)
        => await db.ActivityLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
}
