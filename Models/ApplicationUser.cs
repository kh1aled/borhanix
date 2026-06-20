using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DepiLms.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(16)]
    public string AvatarColor { get; set; } = "#2563eb";

    [MaxLength(240)]
    public string? ProfilePhotoUrl { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public bool IsApproved { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastActiveAt { get; set; }

    public StudentProfile? StudentProfile { get; set; }
    public InstructorProfile? InstructorProfile { get; set; }
    public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<AiConversation> AiConversations { get; set; } = new List<AiConversation>();
}
