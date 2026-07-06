using DepiLms.Models;
using System.ComponentModel.DataAnnotations;

namespace DepiLms.ViewModels;

public class LessonProgressUpdateModel
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int LessonId { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxWatchedSeconds { get; set; }
}

public class LessonProgressDashboardViewModel
{
    public Course Course { get; set; } = default!;
    public IReadOnlyList<Lesson> Lessons { get; set; } = [];
    public IReadOnlyList<ApplicationUser> Students { get; set; } = [];
    public IReadOnlyDictionary<(int LessonId, string StudentId), LessonProgressInfo> Progress { get; set; }
        = new Dictionary<(int, string), LessonProgressInfo>();
}
