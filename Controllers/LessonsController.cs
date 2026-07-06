using DepiLms.Data;
using DepiLms.Models;
using DepiLms.Services;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize]
public class LessonsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ILessonProgressService progressService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> TrackProgress([FromBody] LessonProgressUpdateModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Invalid lesson progress request." });
        }

        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        try
        {
            var result = await progressService.UpdateProgressAsync(
                model.CourseId,
                model.LessonId,
                userId,
                model.MaxWatchedSeconds);
            return Json(new
            {
                result.MaxWatchedSeconds,
                result.ViewingPercent,
                result.IsComplete,
                completedAt = result.CompletedAt
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    public async Task<IActionResult> Progress(int courseId)
    {
        var course = await db.Courses
            .Include(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Enrollments)
            .ThenInclude(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == courseId);

        if (course is null)
        {
            return NotFound();
        }

        var userId = userManager.GetUserId(User);
        if (!User.IsInRole(AppRoles.Admin) && course.CreatedById != userId)
        {
            return Forbid();
        }

        var lessons = course.Modules
            .OrderBy(x => x.SortOrder)
            .SelectMany(x => x.Lessons.OrderBy(l => l.SortOrder))
            .ToList();

        var students = course.Enrollments
            .Where(x => x.Status == EnrollmentStatus.Approved)
            .Select(x => x.Student)
            .OrderBy(x => x.FullName)
            .ToList();

        var studentIds = students.Select(x => x.Id).ToList();
        var lessonIds = lessons.Select(x => x.Id).ToList();

        var records = await db.LessonProgressRecords
            .Where(x => lessonIds.Contains(x.LessonId) && studentIds.Contains(x.StudentId))
            .ToListAsync();

        var progress = records.ToDictionary(
            x => (x.LessonId, x.StudentId),
            x => new LessonProgressInfo
            {
                MaxWatchedSeconds = x.MaxWatchedSeconds,
                ViewingPercent = x.ViewingPercent,
                IsComplete = x.CompletedAt is not null
            });

        return View(new LessonProgressDashboardViewModel
        {
            Course = course,
            Lessons = lessons,
            Students = students,
            Progress = progress
        });
    }
}
