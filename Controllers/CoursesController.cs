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
public class CoursesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICourseCompletionService completionService) :Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var enrollments = await db.Enrollments
            .Where(x => x.StudentId == userId)
            .ToDictionaryAsync(x => x.CourseId, x => x.Status);
        var cartCourseIds = await db.CartItems
            .Where(x => x.StudentId == userId)
            .Select(x => x.CourseId)
            .ToListAsync();
        var savedCourseIds = await db.SavedCourses
            .Where(x => x.StudentId == userId)
            .Select(x => x.CourseId)
            .ToListAsync();

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
            CoverPhotoPath = course.CoverPhotoPath,
            Price = course.Price,
            Currency = course.Currency,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(x => x.Lessons.Count),
            EnrollmentCount = course.Enrollments.Count(x => x.Status == EnrollmentStatus.Approved),
            EnrollmentStatus = enrollments.TryGetValue(course.Id, out var status) ? status : null,
            IsInCart = cartCourseIds.Contains(course.Id),
            IsSaved = savedCourseIds.Contains(course.Id)
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
        var isInCart = await db.CartItems.AnyAsync(x => x.CourseId == id && x.StudentId == userId);
        var isSaved = await db.SavedCourses.AnyAsync(x => x.CourseId == id && x.StudentId == userId);
        var canManage = User.IsInRole(AppRoles.Admin) || course.CreatedById == userId;

        var quizAttemptIds = new Dictionary<int, int>();
        var completedLessonIds = new HashSet<int>();
        var lessonProgressMap = new Dictionary<int, LessonProgressInfo>();
        CourseCompletionStatus? completionStatus = null;
        var canGenerateCertificate = false;
        int? certificateId = null;

        if (userId is not null && User.IsInRole(AppRoles.Student))
        {
            var progressRecords = await db.LessonProgressRecords
                .Where(x => x.StudentId == userId && x.Lesson.CourseModule.CourseId == id)
                .ToListAsync();

            foreach (var record in progressRecords)
            {
                lessonProgressMap[record.LessonId] = new LessonProgressInfo
                {
                    MaxWatchedSeconds = record.MaxWatchedSeconds,
                    ViewingPercent = record.ViewingPercent,
                    IsComplete = record.CompletedAt is not null
                };

                if (record.CompletedAt is not null)
                {
                    completedLessonIds.Add(record.LessonId);
                }
            }

            quizAttemptIds = await db.QuizAttempts
                .Where(x => x.StudentId == userId && x.Quiz.CourseId == id)
                .ToDictionaryAsync(x => x.QuizId, x => x.Id);

            if (enrollment?.Status == EnrollmentStatus.Approved)
            {
                completionStatus = await completionService.GetStatusAsync(id, userId);
                canGenerateCertificate = completionStatus.IsComplete;
            }

            certificateId = await db.CourseCertificates
                .Where(x => x.CourseId == id && x.StudentId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        return View(new CourseDetailsViewModel
        {
            Course = course,
            Enrollment = enrollment,
            CanRequestEnrollment = User.IsInRole(AppRoles.Student) && enrollment is null,
            IsInCart = isInCart,
            IsSaved = isSaved,
            CanManage = canManage,
            QuizAttemptIds = quizAttemptIds,
            CompletedLessonIds = completedLessonIds,
            LessonProgressMap = lessonProgressMap,
            CompletionStatus = completionStatus,
            CanGenerateCertificate = canGenerateCertificate && certificateId is null,
            CertificateId = certificateId,
            CreatedBy = course.CreatedBy
        });
    }

    public async Task<IActionResult> Learn(int id)
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
        var isInCart = await db.CartItems.AnyAsync(x => x.CourseId == id && x.StudentId == userId);
        var isSaved = await db.SavedCourses.AnyAsync(x => x.CourseId == id && x.StudentId == userId);
        var canManage = User.IsInRole(AppRoles.Admin) || course.CreatedById == userId;

        var quizAttemptIds = new Dictionary<int, int>();
        var completedLessonIds = new HashSet<int>();
        var lessonProgressMap = new Dictionary<int, LessonProgressInfo>();
        CourseCompletionStatus? completionStatus = null;
        var canGenerateCertificate = false;
        int? certificateId = null;

        if (userId is not null && User.IsInRole(AppRoles.Student))
        {
            var progressRecords = await db.LessonProgressRecords
                .Where(x => x.StudentId == userId && x.Lesson.CourseModule.CourseId == id)
                .ToListAsync();

            foreach (var record in progressRecords)
            {
                lessonProgressMap[record.LessonId] = new LessonProgressInfo
                {
                    MaxWatchedSeconds = record.MaxWatchedSeconds,
                    ViewingPercent = record.ViewingPercent,
                    IsComplete = record.CompletedAt is not null
                };

                if (record.CompletedAt is not null)
                {
                    completedLessonIds.Add(record.LessonId);
                }
            }

            quizAttemptIds = await db.QuizAttempts
                .Where(x => x.StudentId == userId && x.Quiz.CourseId == id)
                .ToDictionaryAsync(x => x.QuizId, x => x.Id);

            if (enrollment?.Status == EnrollmentStatus.Approved)
            {
                completionStatus = await completionService.GetStatusAsync(id, userId);
                canGenerateCertificate = completionStatus.IsComplete;
            }

            certificateId = await db.CourseCertificates
                .Where(x => x.CourseId == id && x.StudentId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        return View(new CourseDetailsViewModel
        {
            Course = course,
            Enrollment = enrollment,
            CanRequestEnrollment = User.IsInRole(AppRoles.Student) && enrollment is null,
            IsInCart = isInCart,
            IsSaved = isSaved,
            CanManage = canManage,
            QuizAttemptIds = quizAttemptIds,
            CompletedLessonIds = completedLessonIds,
            LessonProgressMap = lessonProgressMap,
            CompletionStatus = completionStatus,
            CanGenerateCertificate = canGenerateCertificate && certificateId is null,
            CertificateId = certificateId,
            CreatedBy = course.CreatedBy
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
            TempData["Status"] = "Enrollment request sent. An instructor or admin must approve it.";
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
