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
public class QuizzesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Take(int id)
    {
        var quiz = await LoadQuizAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (!await CanViewCourseAsync(quiz.Course))
        {
            return Forbid();
        }

        var userId = userManager.GetUserId(User);
        if (User.IsInRole(AppRoles.Student) && userId is not null)
        {
            var existingAttempt = await db.QuizAttempts
                .FirstOrDefaultAsync(x => x.QuizId == quiz.Id && x.StudentId == userId);

            if (existingAttempt is not null)
            {
                return RedirectToAction(nameof(Review), new { id = existingAttempt.Id });
            }
        }

        return View(new QuizTakeViewModel { QuizId = quiz.Id, Quiz = quiz });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Submit(QuizTakeViewModel model)
    {
        var quiz = await LoadQuizAsync(model.QuizId);
        var userId = userManager.GetUserId(User);
        if (quiz is null || userId is null)
        {
            return NotFound();
        }

        if (!await IsApprovedStudentAsync(quiz.CourseId, userId))
        {
            return Forbid();
        }

        var existingAttempt = await db.QuizAttempts
            .FirstOrDefaultAsync(x => x.QuizId == quiz.Id && x.StudentId == userId);

        if (existingAttempt is not null)
        {
            return RedirectToAction(nameof(Review), new { id = existingAttempt.Id });
        }

        decimal score = 0;
        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            StudentId = userId,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        foreach (var question in quiz.Questions)
        {
            model.SelectedOptions.TryGetValue(question.Id, out var optionId);
            var selected = question.Options.FirstOrDefault(x => x.Id == optionId);
            var isCorrect = selected?.IsCorrect == true;
            if (isCorrect)
            {
                score += question.Points;
            }

            attempt.Answers.Add(new QuizAnswer
            {
                QuizQuestionId = question.Id,
                QuizOptionId = selected?.Id,
                IsCorrect = isCorrect
            });
        }

        attempt.Score = score;
        db.QuizAttempts.Add(attempt);

        db.GradeItems.Add(new GradeItem
        {
            CourseId = quiz.CourseId,
            StudentId = userId,
            Title = $"Quiz: {quiz.Title}",
            Category = "Quiz",
            Score = score,
            MaxScore = quiz.Questions.Sum(x => x.Points)
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Review), new { id = attempt.Id });
    }

    public async Task<IActionResult> Review(int id)
    {
        var attempt = await LoadAttemptForReviewAsync(id);
        if (attempt is null)
        {
            return NotFound();
        }

        if (!CanViewAttempt(attempt))
        {
            return Forbid();
        }

        return View("Review", BuildReviewViewModel(attempt));
    }

    public Task<IActionResult> Result(int id) => Review(id);

    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> History()
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        var attempts = await db.QuizAttempts
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Course)
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Questions)
            .Where(x => x.StudentId == userId && x.SubmittedAt != null)
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync();

        return View(new QuizHistoryViewModel { Attempts = attempts });
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    public async Task<IActionResult> Attempts(int id)
    {
        var quiz = await db.Quizzes
            .Include(x => x.Course)
            .Include(x => x.Attempts)
            .ThenInclude(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (quiz is null)
        {
            return NotFound();
        }

        if (!CanManage(quiz.Course))
        {
            return Forbid();
        }

        return View(new QuizAttemptsViewModel
        {
            Quiz = quiz,
            Attempts = quiz.Attempts.OrderByDescending(x => x.SubmittedAt).ToList()
        });
    }

    private async Task<QuizAttempt?> LoadAttemptForReviewAsync(int id)
        => await db.QuizAttempts
            .Include(x => x.Student)
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Course)
            .Include(x => x.Quiz)
            .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id);

    private QuizReviewViewModel BuildReviewViewModel(QuizAttempt attempt)
    {
        var userId = userManager.GetUserId(User);
        var questions = attempt.Quiz.Questions
            .OrderBy(x => x.SortOrder)
            .Select(question =>
            {
                var answer = attempt.Answers.FirstOrDefault(x => x.QuizQuestionId == question.Id);
                return new QuizQuestionReviewItem
                {
                    Question = question,
                    Answer = answer,
                    SelectedOption = question.Options.FirstOrDefault(x => x.Id == answer?.QuizOptionId),
                    CorrectOption = question.Options.FirstOrDefault(x => x.IsCorrect),
                    IsCorrect = answer?.IsCorrect == true
                };
            })
            .ToList();

        return new QuizReviewViewModel
        {
            Attempt = attempt,
            MaxScore = attempt.Quiz.Questions.Sum(x => x.Points),
            Questions = questions,
            IsOwner = attempt.StudentId == userId
        };
    }

    private bool CanViewAttempt(QuizAttempt attempt)
    {
        var userId = userManager.GetUserId(User);
        return CanManage(attempt.Quiz.Course) || attempt.StudentId == userId;
    }

    private async Task<Quiz?> LoadQuizAsync(int id)
        => await db.Quizzes
            .Include(x => x.Course)
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);

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
}
