using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            UserCount = await db.Users.CountAsync(),
            CourseCount = await db.Courses.CountAsync(),
            PendingEnrollments = await db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Pending),
            AttendanceSessions = await db.AttendanceSessions.CountAsync(),
            RecentUsers = await db.Users.OrderByDescending(x => x.CreatedAt).Take(12).ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Users()
    {
        var users = await db.Users.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var rows = new List<AdminUserRowViewModel>();
        foreach (var user in users)
        {
            rows.Add(new AdminUserRowViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = string.Join(", ", await userManager.GetRolesAsync(user)),
                IsApproved = user.IsApproved,
                CreatedAt = user.CreatedAt,
                LastActiveAt = user.LastActiveAt
            });
        }

        return View(new AdminUsersViewModel { Users = rows });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(AdminUserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "User update failed validation.";
            return RedirectToAction(nameof(Users));
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.IsApproved = model.IsApproved;
        user.EmailConfirmed = true;

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            await userManager.SetEmailAsync(user, model.Email);
            await userManager.SetUserNameAsync(user, model.Email);
        }

        await userManager.UpdateAsync(user);

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (model.Role is AppRoles.Admin or AppRoles.Instructor or AppRoles.Student)
        {
            await userManager.AddToRoleAsync(user, model.Role);
        }

        TempData["Status"] = "User updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsApproved = true;
        await userManager.UpdateAsync(user);
        TempData["Status"] = $"{user.FullName} approved.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentAdminId = userManager.GetUserId(User);
        if (id == currentAdminId)
        {
            TempData["Error"] = "You cannot delete your own admin account while signed in.";
            return RedirectToAction(nameof(Users));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        db.AttendanceRecords.RemoveRange(db.AttendanceRecords.Where(x => x.StudentId == id));
        db.GradeItems.RemoveRange(db.GradeItems.Where(x => x.StudentId == id));
        db.AssignmentSubmissions.RemoveRange(db.AssignmentSubmissions.Where(x => x.StudentId == id));
        db.QuizAttempts.RemoveRange(db.QuizAttempts.Where(x => x.StudentId == id));
        db.Enrollments.RemoveRange(db.Enrollments.Where(x => x.StudentId == id || x.ReviewedById == id));
        db.StudentProfiles.RemoveRange(db.StudentProfiles.Where(x => x.ApplicationUserId == id));
        db.InstructorProfiles.RemoveRange(db.InstructorProfiles.Where(x => x.ApplicationUserId == id));
        db.AiConversations.RemoveRange(db.AiConversations.Where(x => x.UserId == id));

        var ownedCourses = await db.Courses.Where(x => x.CreatedById == id).ToListAsync();
        foreach (var course in ownedCourses)
        {
            course.CreatedById = currentAdminId!;
        }

        await db.SaveChangesAsync();
        var result = await userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "Status" : "Error"] = result.Succeeded
            ? "User deleted."
            : string.Join("; ", result.Errors.Select(x => x.Description));

        return RedirectToAction(nameof(Users));
    }
}
