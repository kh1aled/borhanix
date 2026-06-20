using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var role = User.IsInRole(AppRoles.Admin)
            ? AppRoles.Admin
            : User.IsInRole(AppRoles.Instructor)
                ? AppRoles.Instructor
                : AppRoles.Student;

        var model = role switch
        {
            AppRoles.Admin => await BuildAdminDashboardAsync(user),
            AppRoles.Instructor => await BuildInstructorDashboardAsync(user),
            _ => await BuildStudentDashboardAsync(user)
        };

        return View(model);
    }

    private async Task<DashboardViewModel> BuildStudentDashboardAsync(ApplicationUser user)
    {
        var enrollments = await db.Enrollments
            .Include(x => x.Course)
            .ThenInclude(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Where(x => x.StudentId == user.Id)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync();

        var approvedCourseIds = enrollments.Where(x => x.Status == EnrollmentStatus.Approved).Select(x => x.CourseId).ToList();
        var gradeItems = await db.GradeItems.Where(x => x.StudentId == user.Id).ToListAsync();
        var attendance = await db.AttendanceRecords.Where(x => x.StudentId == user.Id).ToListAsync();

        return new DashboardViewModel
        {
            Role = AppRoles.Student,
            UserName = user.FullName,
            Stats =
            [
                new("Active courses", approvedCourseIds.Count.ToString(), "Approved enrollments", "book-open", "cyan"),
                new("Pending approvals", enrollments.Count(x => x.Status == EnrollmentStatus.Pending).ToString(), "Waiting for instructors", "clock", "amber"),
                new("Grade average", gradeItems.Count == 0 ? "-" : $"{gradeItems.Average(x => x.Score / x.MaxScore) * 100:0}%", "Across graded work", "chart-no-axes-column", "green"),
                new("Attendance scans", attendance.Count.ToString(), "QR/ID card records", "qr-code", "violet")
            ],
            Courses = enrollments.Select(x => ToCard(x.Course, x.Status)).ToList(),
            Activity = gradeItems
                .OrderByDescending(x => x.IssuedAt)
                .Take(5)
                .Select(x => new ActivityItem(x.Title, $"{x.Score:0}/{x.MaxScore:0} points", "Grade", x.IssuedAt))
                .ToList()
        };
    }

    private async Task<DashboardViewModel> BuildInstructorDashboardAsync(ApplicationUser user)
    {
        var courses = await db.Courses
            .Include(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Enrollments)
            .Where(x => x.CreatedById == user.Id)
            .OrderBy(x => x.Title)
            .ToListAsync();

        var courseIds = courses.Select(x => x.Id).ToList();
        var pending = await db.Enrollments.CountAsync(x => courseIds.Contains(x.CourseId) && x.Status == EnrollmentStatus.Pending);
        var sessions = await db.AttendanceSessions.CountAsync(x => courseIds.Contains(x.CourseId));
        var submissions = await db.AssignmentSubmissions
            .CountAsync(x => courseIds.Contains(x.Assignment.CourseId) && x.Score == null);

        return new DashboardViewModel
        {
            Role = AppRoles.Instructor,
            UserName = user.FullName,
            Stats =
            [
                new("My courses", courses.Count.ToString(), "Published and draft", "presentation", "cyan"),
                new("Pending approvals", pending.ToString(), "Enrollment requests", "user-check", "amber"),
                new("Attendance sessions", sessions.ToString(), "QR-enabled classes", "scan-line", "green"),
                new("To grade", submissions.ToString(), "Assignment submissions", "clipboard-check", "violet")
            ],
            Courses = courses.Select(x => ToCard(x, null)).ToList(),
            Activity = await db.Announcements
                .Where(x => courseIds.Contains(x.CourseId))
                .OrderByDescending(x => x.PublishedAt)
                .Take(5)
                .Select(x => new ActivityItem(x.Title, x.Course.Title, "Announcement", x.PublishedAt))
                .ToListAsync()
        };
    }

    private async Task<DashboardViewModel> BuildAdminDashboardAsync(ApplicationUser user)
    {
        return new DashboardViewModel
        {
            Role = AppRoles.Admin,
            UserName = user.FullName,
            Stats =
            [
                new("Users", (await db.Users.CountAsync()).ToString(), "Students, instructors, admins", "users", "cyan"),
                new("Courses", (await db.Courses.CountAsync()).ToString(), "All LMS courses", "layers-3", "green"),
                new("Pending enrollments", (await db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Pending)).ToString(), "Need review", "badge-alert", "amber"),
                new("AI conversations", (await db.AiConversations.CountAsync()).ToString(), "OpenRouter usage trail", "sparkles", "violet")
            ],
            Courses = (await db.Courses
                .Include(x => x.Modules)
                .ThenInclude(x => x.Lessons)
                .Include(x => x.Enrollments)
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToListAsync())
                .Select(x => ToCard(x, null))
                .ToList(),
            Activity = await db.Enrollments
                .Include(x => x.Course)
                .Include(x => x.Student)
                .OrderByDescending(x => x.RequestedAt)
                .Take(5)
                .Select(x => new ActivityItem(x.Student.FullName, $"{x.Status} request for {x.Course.Title}", "Enrollment", x.RequestedAt))
                .ToListAsync()
        };
    }

    private static CourseCardViewModel ToCard(Course course, EnrollmentStatus? status)
        => new()
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            Summary = course.Summary,
            Category = course.Category,
            Level = course.Level,
            AccentColor = course.AccentColor,
            HeroImageUrl = course.HeroImageUrl,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(x => x.Lessons.Count),
            EnrollmentCount = course.Enrollments.Count(x => x.Status == EnrollmentStatus.Approved),
            EnrollmentStatus = status
        };
}
