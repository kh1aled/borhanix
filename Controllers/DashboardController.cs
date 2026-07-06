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
        var availableSessions = await db.AttendanceSessions.CountAsync(x =>
            x.Course.Enrollments.Any(e => e.StudentId == user.Id && e.Status == EnrollmentStatus.Approved));
        var completedLessons = await db.LessonProgressRecords.CountAsync(x => x.StudentId == user.Id && x.CompletedAt != null);

        return new DashboardViewModel
        {
            Role = AppRoles.Student,
            UserName = user.FullName,
            Summary = "Keep learning, track your attendance, and continue approved courses from one place.",
            Stats =
            [
                new("Active courses", approvedCourseIds.Count.ToString(), "book-open"),
                new("Pending approvals", enrollments.Count(x => x.Status == EnrollmentStatus.Pending).ToString(), "clock"),
                new("Grade average", gradeItems.Count == 0 ? "-" : $"{gradeItems.Average(x => x.Score / x.MaxScore) * 100:0}%", "chart-no-axes-column"),
                new("Attendance rate", availableSessions == 0 ? "0%" : $"{Math.Round((decimal)attendance.Count / availableSessions * 100, 1)}%", "qr-code"),
                new("Completed lessons", completedLessons.ToString(), "check-circle-2")
            ],
            Actions =
            [
                new("Browse courses", "Courses", "Index", "book-open"),
                new("My ID card", "Attendance", "StudentCard", "id-card", "button button-ghost"),
                new("Quiz history", "Quizzes", "History", "history", "button button-ghost")
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
        var scans = await db.AttendanceRecords.CountAsync(x => courseIds.Contains(x.AttendanceSession.CourseId));
        var submissions = await db.AssignmentSubmissions
            .CountAsync(x => courseIds.Contains(x.Assignment.CourseId) && x.Score == null);
        var approvedStudents = await db.Enrollments.CountAsync(x => courseIds.Contains(x.CourseId) && x.Status == EnrollmentStatus.Approved);

        return new DashboardViewModel
        {
            Role = AppRoles.Instructor,
            UserName = user.FullName,
            Summary = "Manage courses, approve students, scan attendance, and review learning activity.",
            Stats =
            [
                new("My courses", courses.Count.ToString(), "presentation"),
                new("Pending approvals", pending.ToString(), "user-check"),
                new("Attendance sessions", sessions.ToString(), "scan-line"),
                new("Attendance scans", scans.ToString(), "qr-code"),
                new("Approved students", approvedStudents.ToString(), "users-round"),
                new("To grade", submissions.ToString(), "clipboard-check")
            ],
            Actions =
            [
                new("Instructor studio", "InstructorStudio", "Index", "presentation"),
                new("Enrollment approvals", "Enrollments", "Pending", "user-check", "button button-ghost"),
                new("Attendance scanner", "Attendance", "Scanner", "scan-qr-code", "button button-ghost"),
                new("Attendance sessions", "Attendance", "Index", "clipboard-list", "button button-ghost")
            ],
            Courses = courses.Select(x => ToCard(x, null)).ToList(),
            Activity = await db.AttendanceRecords
                .Include(x => x.AttendanceSession)
                .Include(x => x.Student)
                .Where(x => courseIds.Contains(x.AttendanceSession.CourseId))
                .OrderByDescending(x => x.CapturedAt)
                .Take(5)
                .Select(x => new ActivityItem(x.Student.FullName, $"{x.Status} scan for {x.AttendanceSession.Title}", "Attendance", x.CapturedAt))
                .ToListAsync()
        };
    }

    private async Task<DashboardViewModel> BuildAdminDashboardAsync(ApplicationUser user)
    {
        var students = await userManager.GetUsersInRoleAsync(AppRoles.Student);
        var instructors = await userManager.GetUsersInRoleAsync(AppRoles.Instructor);
        var instructorsPendingCount = instructors.Count(x => !x.IsApproved);
        var instructorsApprovedCount = instructors.Count(x => x.IsApproved);
        var pendingUsers = await db.Users.CountAsync(x => !x.IsApproved);
        var attendanceScans = await db.AttendanceRecords.CountAsync();

        return new DashboardViewModel
        {
            Role = AppRoles.Admin,
            UserName = user.FullName,
            Summary = "Watch the full platform, approve accounts and enrollments, and audit attendance activity.",
            Stats =
            [
                new("Users", (await db.Users.CountAsync()).ToString(), "users"),
                new("Pending users", pendingUsers.ToString(), "user-cog"),
                new("Students", students.Count.ToString(), "graduation-cap"),
                new("Instructors", instructors.Count.ToString(), "user-round"),
                new("Instructor approvals", instructorsPendingCount.ToString(), "badge-alert"),
                new("Approved instructors", instructorsApprovedCount.ToString(), "shield-check"),
                new("Courses", (await db.Courses.CountAsync()).ToString(), "book-open"),
                new("Pending enrollments", (await db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Pending)).ToString(), "badge-alert"),
                new("Attendance scans", attendanceScans.ToString(), "qr-code"),
                new("AI conversations", (await db.AiConversations.CountAsync()).ToString(), "sparkles")
            ],
            Actions =
            [
                new("Approve enrollments", "Enrollments", "Pending", "user-check"),
                new("Manage users", "Admin", "Users", "users-round", "button button-ghost"),
                new("Manage courses", "InstructorStudio", "Index", "book-open-check", "button button-ghost"),
                new("Attendance reports", "Attendance", "Index", "clipboard-list", "button button-ghost")
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
            CoverPhotoPath = course.CoverPhotoPath,
            Price = course.Price,
            Currency = course.Currency,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(x => x.Lessons.Count),
            EnrollmentCount = course.Enrollments.Count(x => x.Status == EnrollmentStatus.Approved),
            EnrollmentStatus = status
        };
}
