using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize]
public class AssignmentsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var assignment = await db.Assignments
            .Include(x => x.Course)
            .Include(x => x.Submissions)
            .ThenInclude(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assignment is null)
        {
            return NotFound();
        }

        if (!await CanViewCourseAsync(assignment.Course))
        {
            return Forbid();
        }

        var userId = userManager.GetUserId(User);
        var canGrade = CanManage(assignment.Course);

        return View(new AssignmentDetailsViewModel
        {
            Assignment = assignment,
            MySubmission = assignment.Submissions.FirstOrDefault(x => x.StudentId == userId),
            Submissions = canGrade ? assignment.Submissions.OrderByDescending(x => x.SubmittedAt).ToList() : [],
            CanSubmit = User.IsInRole(AppRoles.Student) && await IsApprovedStudentAsync(assignment.CourseId, userId),
            CanGrade = canGrade
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Submit(AssignmentSubmitViewModel model)
    {
        var assignment = await db.Assignments.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == model.AssignmentId);
        var userId = userManager.GetUserId(User);
        if (assignment is null || userId is null)
        {
            return NotFound();
        }

        if (!await IsApprovedStudentAsync(assignment.CourseId, userId))
        {
            return Forbid();
        }

        var fileUrl = await SaveSubmissionFileAsync(model.SubmissionFile);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Submission file must be PDF, ZIP, DOCX, PNG, JPG, or a plain URL.";
            return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
        }

        var submission = await db.AssignmentSubmissions
            .FirstOrDefaultAsync(x => x.AssignmentId == model.AssignmentId && x.StudentId == userId);

        if (submission is null)
        {
            db.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = model.AssignmentId,
                StudentId = userId,
                FileUrl = fileUrl ?? model.FileUrl,
                Notes = model.Notes
            });
        }
        else
        {
            submission.FileUrl = fileUrl ?? model.FileUrl ?? submission.FileUrl;
            submission.Notes = model.Notes;
            submission.SubmittedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        TempData["Status"] = "Assignment submitted.";
        return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    public async Task<IActionResult> Grade(SubmissionGradeViewModel model)
    {
        var submission = await db.AssignmentSubmissions
            .Include(x => x.Assignment)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == model.SubmissionId);

        if (submission is null)
        {
            return NotFound();
        }

        if (!CanManage(submission.Assignment.Course))
        {
            return Forbid();
        }

        submission.Score = Math.Clamp(model.Score, 0, submission.Assignment.MaxPoints);
        submission.Feedback = model.Feedback;

        var gradeTitle = $"Assignment: {submission.Assignment.Title}";
        var grade = await db.GradeItems.FirstOrDefaultAsync(x =>
            x.CourseId == submission.Assignment.CourseId &&
            x.StudentId == submission.StudentId &&
            x.Title == gradeTitle);

        if (grade is null)
        {
            db.GradeItems.Add(new GradeItem
            {
                CourseId = submission.Assignment.CourseId,
                StudentId = submission.StudentId,
                Title = gradeTitle,
                Category = "Assignment",
                Score = submission.Score.Value,
                MaxScore = submission.Assignment.MaxPoints
            });
        }
        else
        {
            grade.Score = submission.Score.Value;
            grade.MaxScore = submission.Assignment.MaxPoints;
            grade.IssuedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        TempData["Status"] = "Submission graded.";
        return RedirectToAction(nameof(Details), new { id = submission.AssignmentId });
    }

    private async Task<bool> CanViewCourseAsync(Course course)
    {
        if (CanManage(course))
        {
            return true;
        }

        var userId = userManager.GetUserId(User);
        return await IsApprovedStudentAsync(course.Id, userId);
    }

    private bool CanManage(Course course)
    {
        var userId = userManager.GetUserId(User);
        return User.IsInRole(AppRoles.Admin) || course.CreatedById == userId;
    }

    private async Task<bool> IsApprovedStudentAsync(int courseId, string? userId)
        => userId is not null && await db.Enrollments.AnyAsync(x =>
            x.CourseId == courseId &&
            x.StudentId == userId &&
            x.Status == EnrollmentStatus.Approved);

    private async Task<string?> SaveSubmissionFileAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string> { ".pdf", ".zip", ".docx", ".png", ".jpg", ".jpeg" };
        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError(nameof(AssignmentSubmitViewModel.SubmissionFile), "Unsupported file type.");
            return null;
        }

        var folder = Path.Combine(environment.WebRootPath, "uploads", "submissions");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"/uploads/submissions/{fileName}";
    }
}
