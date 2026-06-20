using System.ComponentModel.DataAnnotations;

namespace DepiLms.Models;

public static class AppRoles
{
    public const string Student = "Student";
    public const string Instructor = "Instructor";
    public const string Admin = "Admin";
}

public enum EnrollmentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum AttendanceStatus
{
    Present = 0,
    Late = 1,
    Excused = 2,
    Absent = 3
}

public enum AttendanceSource
{
    StudentQr = 0,
    Manual = 1,
    IdCard = 2
}

public class StudentProfile
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    [MaxLength(32)]
    public string StudentCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(120)]
    public string Program { get; set; } = "Full Stack .NET";

    [MaxLength(80)]
    public string Level { get; set; } = "Foundation";

    [MaxLength(80)]
    public string? NationalId { get; set; }

    [MaxLength(180)]
    public string? PhotoUrl { get; set; }

    [MaxLength(120)]
    public string? EmergencyContact { get; set; }
}

public class InstructorProfile
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    [MaxLength(120)]
    public string Department { get; set; } = "Software Engineering";

    [MaxLength(120)]
    public string Title { get; set; } = "Instructor";

    [MaxLength(240)]
    public string? OfficeHours { get; set; }
}

public class Course
{
    public int Id { get; set; }

    [Required, MaxLength(24)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Category { get; set; } = "Technology";

    [MaxLength(80)]
    public string Level { get; set; } = "Beginner";

    [MaxLength(180)]
    public string? HeroImageUrl { get; set; }

    [MaxLength(16)]
    public string AccentColor { get; set; } = "#14b8a6";

    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = default!;

    public ICollection<CourseModule> Modules { get; set; } = new List<CourseModule>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    public ICollection<GradeItem> GradeItems { get; set; } = new List<GradeItem>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}

public class CourseModule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Summary { get; set; }

    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson
{
    public int Id { get; set; }
    public int CourseModuleId { get; set; }
    public CourseModule CourseModule { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? VideoUrl { get; set; }

    [MaxLength(240)]
    public string? ResourceUrl { get; set; }

    public int DurationMinutes { get; set; } = 30;
    public int SortOrder { get; set; }
    public bool IsPreview { get; set; }
}

public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public string Brief { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? AttachmentUrl { get; set; }

    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public decimal MaxPoints { get; set; } = 100;
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}

public class AssignmentSubmission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = default!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    [MaxLength(240)]
    public string? FileUrl { get; set; }

    public string? Notes { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Quiz
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Summary { get; set; }

    public int TimeLimitMinutes { get; set; } = 20;
    public decimal MaxPoints { get; set; } = 100;
    public bool IsPublished { get; set; } = true;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;
    public string Prompt { get; set; } = string.Empty;
    public decimal Points { get; set; } = 10;
    public int SortOrder { get; set; }
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}

public class QuizOption
{
    public int Id { get; set; }
    public int QuizQuestionId { get; set; }
    public QuizQuestion Question { get; set; } = default!;
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}

public class QuizAnswer
{
    public int Id { get; set; }
    public int QuizAttemptId { get; set; }
    public QuizAttempt Attempt { get; set; } = default!;
    public int QuizQuestionId { get; set; }
    public int? QuizOptionId { get; set; }
    public string? FreeTextAnswer { get; set; }
    public bool? IsCorrect { get; set; }
}

public class Enrollment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public string? InstructorNote { get; set; }
}

public class AttendanceSession
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset SessionDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset OpensAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-10);
    public DateTimeOffset ClosesAt { get; set; } = DateTimeOffset.UtcNow.AddHours(2);

    [MaxLength(64)]
    public string SessionCode { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    public string CreatedById { get; set; } = string.Empty;
    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = default!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.StudentQr;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(240)]
    public string? DeviceInfo { get; set; }
}

public class GradeItem
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; set; } = "Assignment";

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Announcement
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AiConversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    [MaxLength(160)]
    public string Title { get; set; } = "Learning assistant";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

public class AiMessage
{
    public int Id { get; set; }
    public int AiConversationId { get; set; }
    public AiConversation Conversation { get; set; } = default!;

    [MaxLength(24)]
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;
    public int? TokenCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
