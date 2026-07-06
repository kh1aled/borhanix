namespace DepiLms.Services;

public record CourseCompletionStatus(
    bool IsComplete,
    int TotalLessons,
    int CompletedLessons,
    int TotalQuizzes,
    int PassedQuizzes,
    decimal AverageQuizPercent);

public interface ICourseCompletionService
{
    Task<CourseCompletionStatus> GetStatusAsync(int courseId, string studentId);
    Task<bool> IsEligibleForCertificateAsync(int courseId, string studentId);
}
