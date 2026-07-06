using DepiLms.Data;
using DepiLms.Models;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Services;

public interface ILessonProgressService
{
    Task<LessonProgressResult> UpdateProgressAsync(int courseId, int lessonId, string studentId, int maxWatchedSeconds);
}

public record LessonProgressResult(
    int MaxWatchedSeconds,
    decimal ViewingPercent,
    bool IsComplete,
    DateTimeOffset? CompletedAt);

public class LessonProgressService(ApplicationDbContext db) : ILessonProgressService
{
    public const decimal CompletionThresholdPercent = 95m;
    private const int MaxIncrementPerUpdate = 20;

    public async Task<LessonProgressResult> UpdateProgressAsync(int courseId, int lessonId, string studentId, int maxWatchedSeconds)
    {
        if (courseId <= 0 || lessonId <= 0)
        {
            throw new InvalidOperationException("Invalid lesson progress request.");
        }

        if (maxWatchedSeconds < 0)
        {
            throw new InvalidOperationException("Invalid watch time.");
        }

        var lesson = await db.Lessons
            .Include(x => x.CourseModule)
            .FirstOrDefaultAsync(x => x.Id == lessonId)
            ?? throw new InvalidOperationException("Lesson not found.");

        if (lesson.CourseModule.CourseId != courseId)
        {
            throw new InvalidOperationException("Lesson does not belong to this course.");
        }

        var approved = await db.Enrollments.AnyAsync(x =>
            x.CourseId == courseId &&
            x.StudentId == studentId &&
            x.Status == EnrollmentStatus.Approved);

        if (!approved)
        {
            throw new UnauthorizedAccessException("Student is not approved for this course.");
        }

        var totalSeconds = lesson.VideoDurationSeconds ?? (lesson.DurationMinutes > 0 ? lesson.DurationMinutes * 60 : 0);
        if (totalSeconds <= 0)
        {
            totalSeconds = 60;
        }

        var progress = await db.LessonProgressRecords
            .FirstOrDefaultAsync(x => x.LessonId == lessonId && x.StudentId == studentId);

        if (progress?.CompletedAt is not null)
        {
            return new LessonProgressResult(
                progress.MaxWatchedSeconds,
                progress.ViewingPercent,
                true,
                progress.CompletedAt);
        }

        progress ??= new LessonProgress
        {
            LessonId = lessonId,
            StudentId = studentId
        };

        var cappedRequest = Math.Min(maxWatchedSeconds, totalSeconds);
        var allowedMax = progress.MaxWatchedSeconds + MaxIncrementPerUpdate;
        var validatedMax = Math.Min(cappedRequest, allowedMax);
        validatedMax = Math.Max(validatedMax, progress.MaxWatchedSeconds);

        progress.MaxWatchedSeconds = validatedMax;
        progress.ViewingPercent = Math.Round((decimal)validatedMax / totalSeconds * 100, 2);
        progress.LastUpdatedAt = DateTimeOffset.UtcNow;

        if (progress.ViewingPercent >= CompletionThresholdPercent)
        {
            progress.CompletedAt ??= DateTimeOffset.UtcNow;
        }

        if (progress.Id == 0)
        {
            db.LessonProgressRecords.Add(progress);
        }

        await db.SaveChangesAsync();

        return new LessonProgressResult(
            progress.MaxWatchedSeconds,
            progress.ViewingPercent,
            progress.CompletedAt is not null,
            progress.CompletedAt);
    }
}
