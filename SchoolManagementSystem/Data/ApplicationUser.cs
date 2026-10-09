using Microsoft.AspNetCore.Identity;

namespace SchoolManagementSystem.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? TeacherId { get; set; }
        public int? ParentStudentId { get; set; }
    }

}
