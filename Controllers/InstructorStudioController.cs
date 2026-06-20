using DepiLms.Data;
using DepiLms.Models;
using DepiLms.Services;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
public class InstructorStudioController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment,
    IAiQuizGeneratorService quizGenerator) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var courses = await db.Courses
            .Include(x => x.Modules)
            .ThenInclude(x => x.Lessons)
            .Include(x => x.Assignments)
            .Where(x => User.IsInRole(AppRoles.Admin) || x.CreatedById == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(courses);
    }

    [HttpGet]
    public IActionResult CreateCourse() => View(new CourseEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(CourseEditViewModel model)
    {
        var userId = userManager.GetUserId(User);
        if (!ModelState.IsValid || userId is null)
        {
            return View(model);
        }

        db.Courses.Add(new Course
        {
            Code = model.Code.Trim().ToUpperInvariant(),
            Title = model.Title,
            Summary = model.Summary,
            Description = model.Description,
            Category = model.Category,
            Level = model.Level,
            HeroImageUrl = model.HeroImageUrl,
            AccentColor = model.AccentColor,
            IsPublished = model.IsPublished,
            CreatedById = userId
        });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditCourse(int id)
    {
        var course = await GetManageableCourseAsync(id);
        if (course is null)
        {
            return Forbid();
        }

        return View(new CourseEditViewModel
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            Summary = course.Summary,
            Description = course.Description,
            Category = course.Category,
            Level = course.Level,
            HeroImageUrl = course.HeroImageUrl,
            AccentColor = course.AccentColor,
            IsPublished = course.IsPublished
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourse(CourseEditViewModel model)
    {
        if (model.Id is null)
        {
            return BadRequest();
        }

        var course = await GetManageableCourseAsync(model.Id.Value);
        if (course is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        course.Code = model.Code.Trim().ToUpperInvariant();
        course.Title = model.Title;
        course.Summary = model.Summary;
        course.Description = model.Description;
        course.Category = model.Category;
        course.Level = model.Level;
        course.HeroImageUrl = model.HeroImageUrl;
        course.AccentColor = model.AccentColor;
        course.IsPublished = model.IsPublished;

        await db.SaveChangesAsync();
        TempData["Status"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await GetManageableCourseAsync(id);
        if (course is null)
        {
            return Forbid();
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        TempData["Status"] = "Course removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModule(ModuleCreateViewModel model)
    {
        var course = await GetManageableCourseAsync(model.CourseId);
        if (course is null)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            db.CourseModules.Add(new CourseModule
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Summary = model.Summary,
                SortOrder = await db.CourseModules.CountAsync(x => x.CourseId == model.CourseId) + 1
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLesson(LessonCreateViewModel model)
    {
        var module = await db.CourseModules.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == model.ModuleId);
        if (module is null)
        {
            return NotFound();
        }

        if (!CanManage(module.Course))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            var videoUrl = await SaveLessonVideoAsync(model.VideoFile);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Lesson video must be MP4, WEBM, or MOV.";
                return RedirectToAction(nameof(Index));
            }

            db.Lessons.Add(new Lesson
            {
                CourseModuleId = model.ModuleId,
                Title = model.Title,
                Content = model.Content,
                VideoUrl = videoUrl ?? model.VideoUrl,
                ResourceUrl = model.ResourceUrl,
                DurationMinutes = model.DurationMinutes,
                SortOrder = await db.Lessons.CountAsync(x => x.CourseModuleId == model.ModuleId) + 1
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuizFromLesson(int lessonId, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons
            .Include(x => x.CourseModule)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken);

        if (lesson is null)
        {
            return NotFound();
        }

        if (!CanManage(lesson.CourseModule.Course))
        {
            return Forbid();
        }

        var generated = await quizGenerator.GenerateForLessonAsync(lesson, cancellationToken);
        var quiz = new Quiz
        {
            CourseId = lesson.CourseModule.CourseId,
            LessonId = lesson.Id,
            Title = generated.Title,
            Summary = $"AI-generated practice quiz for {lesson.Title}.",
            TimeLimitMinutes = 15,
            MaxPoints = generated.Questions.Count * 10,
            Questions = generated.Questions.Select((question, index) => new QuizQuestion
            {
                Prompt = question.Prompt,
                Points = 10,
                SortOrder = index + 1,
                Options = question.Options.Select(option => new QuizOption
                {
                    Text = option.Text,
                    IsCorrect = option.IsCorrect
                }).ToList()
            }).ToList()
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "AI quiz generated for the lesson.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(AssignmentCreateViewModel model)
    {
        var course = await GetManageableCourseAsync(model.CourseId);
        if (course is null)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            db.Assignments.Add(new Assignment
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Brief = model.Brief,
                DueAt = model.DueAt,
                MaxPoints = model.MaxPoints
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<Course?> GetManageableCourseAsync(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        return course is not null && CanManage(course) ? course : null;
    }

    private bool CanManage(Course course)
    {
        var userId = userManager.GetUserId(User);
        return User.IsInRole(AppRoles.Admin) || course.CreatedById == userId;
    }

    private async Task<string?> SaveLessonVideoAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string> { ".mp4", ".webm", ".mov" };
        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError(nameof(LessonCreateViewModel.VideoFile), "Use MP4, WEBM, or MOV.");
            return null;
        }

        var folder = Path.Combine(environment.WebRootPath, "uploads", "videos");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"/uploads/videos/{fileName}";
    }
}
