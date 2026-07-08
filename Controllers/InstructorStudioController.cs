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
    IAiQuizGeneratorService quizGenerator,
    ICourseImageUploadService imageUpload,
    IVideoDurationService videoDuration) : Controller
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
    public async Task<IActionResult> ManageContent(int id, int? lessonId)
    {
        var course = await db.Courses
            .Include(x => x.Modules)
                .ThenInclude(x => x.Lessons)
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (course is null)
        {
            return NotFound();
        }

        if (!CanManage(course))
        {
            return Forbid();
        }

        Lesson? selectedLesson = null;
        if (lessonId is not null)
        {
            selectedLesson = course.Modules
                .SelectMany(m => m.Lessons)
                .FirstOrDefault(l => l.Id == lessonId);
        }

        ViewBag.SelectedLesson = selectedLesson;
        ViewBag.SelectedModuleId = selectedLesson?.CourseModuleId;

        return View(course);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var course = await GetManageableCourseAsync(id);
        if (course is null)
        {
            return Forbid();
        }

        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync();
        TempData["Status"] = course.IsPublished ? "Course published." : "Course unpublished.";
        return RedirectToAction(nameof(Index));
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

        string? coverPhotoPath = null;
        Console.WriteLine(model.CoverPhoto?.FileName);
        Console.WriteLine(model.CoverPhoto?.Length);
        try
        {
            coverPhotoPath = await imageUpload.SaveCoverPhotoAsync(model.CoverPhoto);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(CourseEditViewModel.CoverPhoto), ex.Message);
            return View(model);
        }
        var code = await GenerateCourseCode(model.Title);

        db.Courses.Add(new Course
        {
            Code = code,
            Title = model.Title,
            Summary = model.Summary,
            Description = model.Description,
            Category = model.Category,
            Level = model.Level,
            HeroImageUrl = model.HeroImageUrl,
            CoverPhotoPath = coverPhotoPath,
            AccentColor = model.AccentColor,
            Price = model.Price,
            Currency = model.Currency.Trim().ToUpperInvariant(),
            IsPublished = model.IsPublished,
            CreatedById = userId
        });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateCourseCode(string title)
    {
        string prefix = new string(title
            .Trim()
            .Where(char.IsLetter)
            .Take(3)
            .ToArray())
            .ToUpper();

        var count = await db.Courses.CountAsync() + 1;

        return $"{prefix}-{count:D4}";
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
            Title = course.Title,
            Summary = course.Summary,
            Description = course.Description,
            Category = course.Category,
            Level = course.Level,
            HeroImageUrl = course.HeroImageUrl,
            CoverPhotoPath = course.CoverPhotoPath,
            AccentColor = course.AccentColor,
            Price = course.Price,
            Currency = course.Currency,
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

        try
        {
            var coverPhotoPath = await imageUpload.SaveCoverPhotoAsync(model.CoverPhoto, course.CoverPhotoPath);
            if (coverPhotoPath is not null)
            {
                course.CoverPhotoPath = coverPhotoPath;
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(CourseEditViewModel.CoverPhoto), ex.Message);
            return View(model);
        }

        course.Title = model.Title;
        course.Summary = model.Summary;
        course.Description = model.Description;
        course.Category = model.Category;
        course.Level = model.Level;
        course.HeroImageUrl = model.HeroImageUrl;
        course.AccentColor = model.AccentColor;
        course.Price = model.Price;
        course.Currency = model.Currency.Trim().ToUpperInvariant();
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

        return RedirectToAction(nameof(ManageContent) , new { id = model.CourseId});
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
                TempData["Error"] = "Lesson video must be MP4, WEBM, MOV, or M4V.";
                return RedirectToAction(nameof(Index));
            }

            var resolvedUrl = videoUrl ?? model.VideoUrl;
            int? videoDurationSeconds = null;
            if (videoUrl is not null && model.VideoFile is not null)
            {
                videoDurationSeconds = await videoDuration.TryGetUploadDurationSecondsAsync(
                    model.VideoFile, videoUrl, model.DurationMinutes);
            }
            else
            {
                videoDurationSeconds = videoDuration.TryGetDurationSeconds(
                    environment.WebRootPath, resolvedUrl, model.DurationMinutes, model.VideoDurationSeconds);
            }
            var newLesson = new Lesson
            {
                CourseModuleId = model.ModuleId,
                Title = model.Title,
                Content = model.Content,
                VideoUrl = resolvedUrl,
                ResourceUrl = model.ResourceUrl,
                DurationMinutes = model.DurationMinutes,
                VideoDurationSeconds = videoDurationSeconds,
                SortOrder = await db.Lessons.CountAsync(x => x.CourseModuleId == model.ModuleId) + 1
            };

            db.Lessons.Add(newLesson);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageContent), new { id = module.CourseId, lessonId = newLesson.Id });

        }

        return RedirectToAction(nameof(ManageContent), new { id = module.CourseId});

    }

    [HttpGet]
    public async Task<IActionResult> EditModule(int id)
    {
        var module = await db.CourseModules.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == id);
        if (module is null)
        {
            return NotFound();
        }

        if (!CanManage(module.Course))
        {
            return Forbid();
        }

        return View(new ModuleEditViewModel
        {
            Id = module.Id,
            CourseId = module.CourseId,
            Title = module.Title,
            Summary = module.Summary
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModule(ModuleEditViewModel model)
    {
        var module = await db.CourseModules.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == model.Id);
        if (module is null)
        {
            return NotFound();
        }

        if (!CanManage(module.Course))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        module.Title = model.Title;
        module.Summary = model.Summary;
        await db.SaveChangesAsync();
        TempData["Status"] = "Module updated.";
        return RedirectToAction(nameof(ManageContent), new { id = module.CourseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteModule(int id)
    {
        var module = await db.CourseModules
            .Include(x => x.Course)
            .Include(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (module is null)
        {
            return NotFound();
        }

        if (!CanManage(module.Course))
        {
            return Forbid();
        }

        foreach (var lesson in module.Lessons)
        {
            await RemoveLessonQuizzesAsync(lesson.Id);
        }

        db.CourseModules.Remove(module);
        await db.SaveChangesAsync();
        TempData["Status"] = "Module removed.";
        return RedirectToAction(nameof(ManageContent), new { id = module.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> EditLesson(int id)
    {
        var lesson = await db.Lessons
            .Include(x => x.CourseModule)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (lesson is null)
        {
            return NotFound();
        }

        if (!CanManage(lesson.CourseModule.Course))
        {
            return Forbid();
        }

        return View(new LessonEditViewModel
        {
            Id = lesson.Id,
            ModuleId = lesson.CourseModuleId,
            CourseId = lesson.CourseModule.CourseId,
            Title = lesson.Title,
            Content = lesson.Content,
            VideoUrl = lesson.VideoUrl,
            ResourceUrl = lesson.ResourceUrl,
            DurationMinutes = lesson.DurationMinutes,
            VideoDurationSeconds = lesson.VideoDurationSeconds
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLesson(LessonEditViewModel model)
    {
        var lesson = await db.Lessons
            .Include(x => x.CourseModule)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (lesson is null)
        {
            return NotFound();
        }

        if (!CanManage(lesson.CourseModule.Course))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var videoUrl = await SaveLessonVideoAsync(model.VideoFile);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Lesson video must be MP4, WEBM, MOV, or M4V.";
            return View(model);
        }

        var resolvedUrl = videoUrl ?? model.VideoUrl;
        if (videoUrl is not null && model.VideoFile is not null)
        {
            lesson.VideoDurationSeconds = await videoDuration.TryGetUploadDurationSecondsAsync(
                model.VideoFile, videoUrl, model.DurationMinutes);
        }
        else if (model.VideoDurationSeconds is > 0 || resolvedUrl != lesson.VideoUrl)
        {
            lesson.VideoDurationSeconds = videoDuration.TryGetDurationSeconds(
                environment.WebRootPath, resolvedUrl, model.DurationMinutes, model.VideoDurationSeconds);
        }

        lesson.Title = model.Title;
        lesson.Content = model.Content;
        lesson.VideoUrl = resolvedUrl;
        lesson.ResourceUrl = model.ResourceUrl;
        lesson.DurationMinutes = model.DurationMinutes;

        await db.SaveChangesAsync();
        TempData["Status"] = "Lesson updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(int id)
    {
        var lesson = await db.Lessons
            .Include(x => x.CourseModule)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (lesson is null)
        {
            return NotFound();
        }

        if (!CanManage(lesson.CourseModule.Course))
        {
            return Forbid();
        }

        await RemoveLessonQuizzesAsync(lesson.Id);
        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync();
        TempData["Status"] = "Lesson removed.";
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
                DeadlineHours = model.DeadlineHours > 0 ? model.DeadlineHours : 48,
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

    private async Task RemoveLessonQuizzesAsync(int lessonId)
    {
        var quizzes = await db.Quizzes.Where(x => x.LessonId == lessonId).ToListAsync();
        if (quizzes.Count > 0)
        {
            db.Quizzes.RemoveRange(quizzes);
        }
    }

    private async Task<string?> SaveLessonVideoAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string> { ".mp4", ".webm", ".mov", ".m4v" };
        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError(nameof(LessonCreateViewModel.VideoFile), "Use MP4, WEBM, MOV, or M4V.");
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
