using DepiLms.Data;
using DepiLms.Models;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Services;

public class CourseCompletionService(ApplicationDbContext db) : ICourseCompletionService
{
    public const decimal PassingPercent = 60m;

    public async Task<CourseCompletionStatus> GetStatusAsync(int courseId, string studentId)
    {
        var lessonIds = await db.Lessons
            .Where(x => x.CourseModule.CourseId == courseId)
            .Select(x => x.Id)
            .ToListAsync();

        var completedLessons = await db.LessonProgressRecords
            .CountAsync(x => x.StudentId == studentId && lessonIds.Contains(x.LessonId) && x.CompletedAt != null);

        var quizzes = await db.Quizzes
            .Include(x => x.Questions)
            .Where(x => x.CourseId == courseId)
            .ToListAsync();

        var attempts = await db.QuizAttempts
            .Where(x => x.StudentId == studentId && x.Quiz.CourseId == courseId && x.SubmittedAt != null)
            .ToListAsync();

        var passedQuizzes = 0;
        decimal percentSum = 0;
        foreach (var quiz in quizzes)
        {
            var attempt = attempts.FirstOrDefault(x => x.QuizId == quiz.Id);
            if (attempt is null)
            {
                continue;
            }

            var maxScore = quiz.Questions.Sum(x => x.Points);
            var percent = maxScore > 0 ? (attempt.Score ?? 0) / maxScore * 100 : 0;
            percentSum += percent;
            if (percent >= PassingPercent)
            {
                passedQuizzes++;
            }
        }

        var averageQuizPercent = quizzes.Count == 0 ? 100 : percentSum / quizzes.Count;
        var lessonsComplete = lessonIds.Count == 0 || completedLessons >= lessonIds.Count;
        var quizzesComplete = quizzes.Count == 0 || passedQuizzes >= quizzes.Count;

        return new CourseCompletionStatus(
            lessonsComplete && quizzesComplete,
            lessonIds.Count,
            completedLessons,
            quizzes.Count,
            passedQuizzes,
            Math.Round(averageQuizPercent, 1));
    }

    public async Task<bool> IsEligibleForCertificateAsync(int courseId, string studentId)
    {
        var approved = await db.Enrollments.AnyAsync(x =>
            x.CourseId == courseId &&
            x.StudentId == studentId &&
            x.Status == EnrollmentStatus.Approved);

        if (!approved)
        {
            return false;
        }

        var status = await GetStatusAsync(courseId, studentId);
        return status.IsComplete;
    }
}
