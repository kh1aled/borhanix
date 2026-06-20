using DepiLms.Models;

namespace DepiLms.ViewModels;

public record StatCard(string Label, string Value, string Hint, string Icon, string Tone);
public record ActivityItem(string Title, string Detail, string Type, DateTimeOffset At);

public class DashboardViewModel
{
    public string Role { get; set; } = AppRoles.Student;
    public string UserName { get; set; } = string.Empty;
    public IReadOnlyCollection<StatCard> Stats { get; set; } = [];
    public IReadOnlyCollection<CourseCardViewModel> Courses { get; set; } = [];
    public IReadOnlyCollection<ActivityItem> Activity { get; set; } = [];
}

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int CourseCount { get; set; }
    public int PendingEnrollments { get; set; }
    public int AttendanceSessions { get; set; }
    public IReadOnlyCollection<ApplicationUser> RecentUsers { get; set; } = [];
}
