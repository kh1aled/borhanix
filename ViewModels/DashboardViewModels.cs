using DepiLms.Models;

namespace DepiLms.ViewModels;

public record StatCard(string Label, string Value, string Icon);
public record ActivityItem(string Title, string Detail, string Type, DateTimeOffset At);
public record DashboardActionItem(string Label, string Controller, string Action, string Icon, string Style = "button");

public class DashboardViewModel
{
    public string Role { get; set; } = AppRoles.Student;
    public string UserName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyCollection<StatCard> Stats { get; set; } = [];
    public IReadOnlyCollection<DashboardActionItem> Actions { get; set; } = [];
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
