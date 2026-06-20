using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize]
public class CoursesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var enrollments = await db.Enrollments
            .Where(x => x.StudentId == userId)
            .ToDictionaryAsync(x => x.CourseId, x => x.Status);

        var courses = await db.Courses
            .Include(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Enrollments)
            .Where(x => x.IsPublished || x.CreatedById == userId || User.IsInRole(AppRoles.Admin))
            .OrderBy(x => x.Title)
            .ToListAsync();

        var model = courses.Select(course => new CourseCardViewModel
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
            EnrollmentStatus = enrollments.TryGetValue(course.Id, out var status) ? status : null
        }).ToList();

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await db.Courses
            .Include(x => x.CreatedBy)
            .Include(x => x.Modules.OrderBy(m => m.SortOrder))
            .ThenInclude(x => x.Lessons.OrderBy(l => l.SortOrder))
            .Include(x => x.Assignments)
            .Include(x => x.Quizzes)
            .ThenInclude(x => x.Questions)
            .Include(x => x.Announcements)
            .Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (course is null)
        {
            return NotFound();
        }

        var userId = userManager.GetUserId(User);
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.CourseId == id && x.StudentId == userId);
        var canManage = User.IsInRole(AppRoles.Admin) || course.CreatedById == userId;

        return View(new CourseDetailsViewModel
        {
            Course = course,
            Enrollment = enrollment,
            CanRequestEnrollment = User.IsInRole(AppRoles.Student) && enrollment is null,
            CanManage = canManage
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Enroll(int id)
    {
        var userId = userManager.GetUserId(User);
        var exists = await db.Enrollments.AnyAsync(x => x.CourseId == id && x.StudentId == userId);
        if (!exists && userId is not null)
        {
            db.Enrollments.Add(new Enrollment
            {
                CourseId = id,
                StudentId = userId,
                Status = EnrollmentStatus.Pending
            });
            await db.SaveChangesAsync();
            TempData["Status"] = "Enrollment request sent. An instructor must approve it.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    public async Task<IActionResult> Manage()
    {
        var userId = userManager.GetUserId(User);
        var query = db.Courses
            .Include(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Enrollments)
            .AsQueryable();

        if (!User.IsInRole(AppRoles.Admin))
        {
            query = query.Where(x => x.CreatedById == userId);
        }

        var courses = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return View(courses);
    }
}
