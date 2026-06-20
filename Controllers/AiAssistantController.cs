using System.Text;
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
public class AiAssistantController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IOpenRouterAiService aiService,
    IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var conversation = await db.AiConversations
            .Include(x => x.Messages.OrderBy(m => m.CreatedAt))
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        return View(new AiAssistantViewModel
        {
            ConversationId = conversation?.Id,
            Messages = conversation?.Messages.OrderBy(x => x.CreatedAt).ToList() ?? [],
            Model = configuration["OpenRouter:Model"] ?? "deepseek/deepseek-chat"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(AiAssistantViewModel viewModel, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(viewModel.Prompt))
        {
            return RedirectToAction(nameof(Index));
        }

        var conversation = await db.AiConversations
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == viewModel.ConversationId && x.UserId == user.Id, cancellationToken);

        if (conversation is null)
        {
            conversation = new AiConversation
            {
                UserId = user.Id,
                Title = viewModel.Prompt.Length > 80 ? viewModel.Prompt[..80] : viewModel.Prompt
            };
            db.AiConversations.Add(conversation);
        }

        var history = conversation.Messages
            .OrderBy(x => x.CreatedAt)
            .Select(x => new AiPromptMessage(x.Role, x.Content))
            .ToList();

        conversation.Messages.Add(new AiMessage
        {
            Role = "user",
            Content = viewModel.Prompt.Trim()
        });

        var context = await BuildPlatformContextAsync(user, cancellationToken);
        var response = await aiService.AskAsync(history, viewModel.Prompt.Trim(), context, cancellationToken);

        conversation.Messages.Add(new AiMessage
        {
            Role = "assistant",
            Content = response.Content,
            TokenCount = response.TotalTokens
        });

        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> BuildPlatformContextAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roleList = await userManager.GetRolesAsync(user);
        var roles = string.Join(", ", roleList);
        var builder = new StringBuilder();
        builder.AppendLine($"Current user: {user.FullName} ({roles}).");

        if (roleList.Contains(AppRoles.Student))
        {
            var enrollments = await db.Enrollments
                .Include(x => x.Course)
                .Where(x => x.StudentId == user.Id)
                .ToListAsync(cancellationToken);
            var grades = await db.GradeItems
                .Include(x => x.Course)
                .Where(x => x.StudentId == user.Id)
                .OrderByDescending(x => x.IssuedAt)
                .Take(8)
                .ToListAsync(cancellationToken);

            builder.AppendLine("Student enrollments:");
            foreach (var enrollment in enrollments)
            {
                builder.AppendLine($"- {enrollment.Course.Code}: {enrollment.Course.Title} ({enrollment.Status})");
            }

            builder.AppendLine("Recent grades:");
            foreach (var grade in grades)
            {
                builder.AppendLine($"- {grade.Course.Code} / {grade.Title}: {grade.Score}/{grade.MaxScore}");
            }
        }

        if (roleList.Contains(AppRoles.Instructor) || roleList.Contains(AppRoles.Admin))
        {
            var query = db.Courses.Include(x => x.Enrollments).AsQueryable();
            if (!roleList.Contains(AppRoles.Admin))
            {
                query = query.Where(x => x.CreatedById == user.Id);
            }

            var courses = await query.Take(10).ToListAsync(cancellationToken);
            builder.AppendLine("Managed courses:");
            foreach (var course in courses)
            {
                builder.AppendLine($"- {course.Code}: {course.Title}; approved students: {course.Enrollments.Count(x => x.Status == EnrollmentStatus.Approved)}; pending: {course.Enrollments.Count(x => x.Status == EnrollmentStatus.Pending)}");
            }
        }

        builder.AppendLine("Platform features: full LMS courses, modules, lessons, assignments, quizzes, grade tracking, enrollment approval, QR student ID, QR attendance, role dashboards.");
        return builder.ToString();
    }
}
