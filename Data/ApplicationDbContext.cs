using DepiLms.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<AssignmentAccess> AssignmentAccessRecords => Set<AssignmentAccess>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<GradeItem> GradeItems => Set<GradeItem>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<SavedCourse> SavedCourses => Set<SavedCourse>();
    public DbSet<SandboxPayment> SandboxPayments => Set<SandboxPayment>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<LessonProgress> LessonProgressRecords => Set<LessonProgress>();
    public DbSet<CourseCertificate> CourseCertificates => Set<CourseCertificate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(x => x.StudentCode).IsUnique();
            entity.HasIndex(x => x.QrToken).IsUnique();
            entity.Property(x => x.StudentCode).HasMaxLength(32);
            entity.Property(x => x.QrToken).HasMaxLength(64);
            entity.HasOne(x => x.User)
                .WithOne(x => x.StudentProfile)
                .HasForeignKey<StudentProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InstructorProfile>(entity =>
        {
            entity.HasOne(x => x.User)
                .WithOne(x => x.InstructorProfile)
                .HasForeignKey<InstructorProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Course>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.AccentColor).HasMaxLength(16);
            entity.Property(x => x.CoverPhotoPath).HasMaxLength(240);
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.HasOne(x => x.CreatedBy)
                .WithMany(x => x.CreatedCourses)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Student)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SavedCourse>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Student)
                .WithMany(x => x.SavedCourses)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.SavedByUsers)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SandboxPayment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.CardLast4).HasMaxLength(4);
            entity.Property(x => x.Reference).HasMaxLength(120);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CourseModule>()
            .HasOne(x => x.Course)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Lesson>()
            .HasOne(x => x.CourseModule)
            .WithMany(x => x.Lessons)
            .HasForeignKey(x => x.CourseModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Enrollment>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Student)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Assignment>()
            .HasOne(x => x.Course)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AssignmentSubmission>(entity =>
        {
            entity.Property(x => x.Score).HasPrecision(8, 2);
            entity.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Assignment)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssignmentAccess>(entity =>
        {
            entity.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Assignment)
                .WithMany(x => x.StudentAccess)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Assignment>()
            .Property(x => x.MaxPoints)
            .HasPrecision(8, 2);

        builder.Entity<Quiz>()
            .HasOne(x => x.Course)
            .WithMany(x => x.Quizzes)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Quiz>()
            .HasOne(x => x.Lesson)
            .WithMany()
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Quiz>()
            .Property(x => x.MaxPoints)
            .HasPrecision(8, 2);

        builder.Entity<QuizQuestion>()
            .HasOne(x => x.Quiz)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizQuestion>()
            .Property(x => x.Points)
            .HasPrecision(8, 2);

        builder.Entity<QuizOption>()
            .HasOne(x => x.Question)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.QuizQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.Property(x => x.Score).HasPrecision(8, 2);
            entity.HasIndex(x => new { x.QuizId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuizAnswer>()
            .HasOne(x => x.Attempt)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttendanceSession>()
            .HasOne(x => x.Course)
            .WithMany(x => x.AttendanceSessions)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasIndex(x => new { x.AttendanceSessionId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.AttendanceSession)
                .WithMany(x => x.Records)
                .HasForeignKey(x => x.AttendanceSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany(x => x.AttendanceRecords)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GradeItem>(entity =>
        {
            entity.Property(x => x.Score).HasPrecision(8, 2);
            entity.Property(x => x.MaxScore).HasPrecision(8, 2);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.GradeItems)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Announcement>()
            .HasOne(x => x.Course)
            .WithMany(x => x.Announcements)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AiConversation>()
            .HasOne(x => x.User)
            .WithMany(x => x.AiConversations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AiMessage>()
            .HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.AiConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasIndex(x => new { x.LessonId, x.StudentId }).IsUnique();
            entity.Property(x => x.ViewingPercent).HasPrecision(8, 2);
            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.ProgressRecords)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CourseCertificate>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
            entity.Property(x => x.CertificateNumber).HasMaxLength(64);
            entity.Property(x => x.FinalGradePercent).HasPrecision(8, 2);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
