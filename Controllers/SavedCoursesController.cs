using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class SavedCoursesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var cartCourseIds = await db.CartItems
            .Where(x => x.StudentId == userId)
            .Select(x => x.CourseId)
            .ToListAsync();
        var enrollments = await db.Enrollments
            .Where(x => x.StudentId == userId)
            .ToDictionaryAsync(x => x.CourseId, x => x.Status);

        var saved = await db.SavedCourses
            .Include(x => x.Course)
            .ThenInclude(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Course.Enrollments)
            .Where(x => x.StudentId == userId)
            .OrderByDescending(x => x.SavedAt)
            .ToListAsync();

        var model = saved.Select(x => new CourseCardViewModel
        {
            Id = x.Course.Id,
            Code = x.Course.Code,
            Title = x.Course.Title,
            Summary = x.Course.Summary,
            Category = x.Course.Category,
            Level = x.Course.Level,
            AccentColor = x.Course.AccentColor,
            HeroImageUrl = x.Course.HeroImageUrl,
            CoverPhotoPath = x.Course.CoverPhotoPath,
            Price = x.Course.Price,
            Currency = x.Course.Currency,
            ModuleCount = x.Course.Modules.Count,
            LessonCount = x.Course.Modules.Sum(m => m.Lessons.Count),
            EnrollmentCount = x.Course.Enrollments.Count(e => e.Status == EnrollmentStatus.Approved),
            EnrollmentStatus = enrollments.TryGetValue(x.CourseId, out var status) ? status : null,
            IsInCart = cartCourseIds.Contains(x.CourseId),
            IsSaved = true
        }).ToList();

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int courseId, string? returnUrl)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var existing = await db.SavedCourses.FirstOrDefaultAsync(x => x.CourseId == courseId && x.StudentId == userId);
        if (existing is null)
        {
            var courseExists = await db.Courses.AnyAsync(x => x.Id == courseId && x.IsPublished);
            if (courseExists)
            {
                db.SavedCourses.Add(new SavedCourse { CourseId = courseId, StudentId = userId });
                TempData["Status"] = "Course saved.";
            }
        }
        else
        {
            db.SavedCourses.Remove(existing);
            TempData["Status"] = "Course removed from saved courses.";
        }

        await db.SaveChangesAsync();
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
