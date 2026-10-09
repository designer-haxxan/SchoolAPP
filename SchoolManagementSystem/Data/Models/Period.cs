using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Models;

public class Period
{
    public int Id { get; set; }
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty; // "Period 1", "Break", "Lunch"
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int OrderIndex { get; set; }
    public bool IsBreak { get; set; }
    public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
}
