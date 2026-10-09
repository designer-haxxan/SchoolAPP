using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Models;

namespace SchoolManagementSystem.Services;

public class AnnouncementService(ApplicationDbContext db)
{
    public async Task<List<Announcement>> GetAllAsync()
        => await db.Announcements
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();

    public async Task<List<Announcement>> GetActiveAsync()
        => await db.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(a => a.PublishedAt)
            .Take(10)
            .ToListAsync();

    public async Task<Announcement> CreateAsync(Announcement announcement)
    {
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();
        return announcement;
    }

    public async Task UpdateAsync(Announcement announcement)
    {
        db.Announcements.Update(announcement);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a != null) { db.Announcements.Remove(a); await db.SaveChangesAsync(); }
    }

    public async Task<List<SchoolEvent>> GetEventsAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = db.SchoolEvents.AsQueryable();
        if (from.HasValue) query = query.Where(e => e.StartDateTime >= from.Value);
        if (to.HasValue) query = query.Where(e => e.StartDateTime <= to.Value);
        return await query.OrderBy(e => e.StartDateTime).ToListAsync();
    }

    public async Task<List<SchoolEvent>> GetUpcomingEventsAsync(int count = 5)
        => await db.SchoolEvents
            .Where(e => e.StartDateTime >= DateTime.Today)
            .OrderBy(e => e.StartDateTime)
            .Take(count)
            .ToListAsync();

    public async Task SaveEventAsync(SchoolEvent ev)
    {
        if (ev.Id == 0) db.SchoolEvents.Add(ev);
        else db.SchoolEvents.Update(ev);
        await db.SaveChangesAsync();
    }

    public async Task DeleteEventAsync(int id)
    {
        var e = await db.SchoolEvents.FindAsync(id);
        if (e != null) { db.SchoolEvents.Remove(e); await db.SaveChangesAsync(); }
    }

    public async Task SaveAsync(Announcement a)
    {
        if (a.Id == 0) { a.PublishedAt = DateTime.Now; db.Announcements.Add(a); }
        else db.Announcements.Update(a);
        await db.SaveChangesAsync();
    }

    public async Task<List<SchoolEvent>> GetAllEventsAsync() =>
        await db.SchoolEvents.OrderBy(e => e.StartDateTime).ToListAsync();
}
