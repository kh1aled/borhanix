using System.ComponentModel.DataAnnotations;
using DepiLms.Models;

namespace DepiLms.ViewModels;

public class AttendanceIndexViewModel
{
    public IReadOnlyCollection<AttendanceSession> Sessions { get; set; } = [];
    public IReadOnlyCollection<AttendanceRecord> Records { get; set; } = [];
    public IReadOnlyCollection<Course> Courses { get; set; } = [];
    public int TotalSessions { get; set; }
    public int TotalScans { get; set; }
    public int ActiveSessions { get; set; }
    public AttendanceSessionCreateViewModel NewSession { get; set; } = new();
}

public class AttendanceSessionCreateViewModel
{
    [Required]
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset SessionDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset OpensAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-15);
    public DateTimeOffset ClosesAt { get; set; } = DateTimeOffset.UtcNow.AddHours(2);
}

public class AttendanceScanViewModel
{
    public int SessionId { get; set; }
    public AttendanceSession? Session { get; set; }
    public IReadOnlyCollection<AttendanceSession> AvailableSessions { get; set; } = [];

    [Required]
    public string Payload { get; set; } = string.Empty;
}

public class StudentCardViewModel
{
    public ApplicationUser User { get; set; } = default!;
    public StudentProfile Profile { get; set; } = default!;
    public string QrPayload { get; set; } = string.Empty;
    public string QrSvg { get; set; } = string.Empty;
    public string QrImageDataUri { get; set; } = string.Empty;
    public int ApprovedCourses { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class AttendanceSessionDetailsViewModel
{
    public AttendanceSession Session { get; set; } = default!;
    public IReadOnlyCollection<AttendanceRecord> Records { get; set; } = [];
    public IReadOnlyCollection<ApplicationUser> MissingStudents { get; set; } = [];
    public int ApprovedStudentCount { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int MissingCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public TimeSpan TotalDuration => Session.ClosesAt - Session.OpensAt;
}
