using DepiLms.Data;
using DepiLms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
public class EnrollmentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Pending()
    {
        var userId = userManager.GetUserId(User);
        var query = db.Enrollments
            .Include(x => x.Course)
            .Include(x => x.Student)
            .Where(x => x.Status == EnrollmentStatus.Pending);

        if (!User.IsInRole(AppRoles.Admin))
        {
            query = query.Where(x => x.Course.CreatedById == userId);
        }

        return View(await query.OrderBy(x => x.RequestedAt).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(int id, EnrollmentStatus status, string? note)
    {
        if (status is not EnrollmentStatus.Approved and not EnrollmentStatus.Rejected)
        {
            return BadRequest();
        }

        var userId = userManager.GetUserId(User);
        var enrollment = await db.Enrollments
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (enrollment is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(AppRoles.Admin) && enrollment.Course.CreatedById != userId)
        {
            return Forbid();
        }

        enrollment.Status = status;
        enrollment.ReviewedAt = DateTimeOffset.UtcNow;
        enrollment.ReviewedById = userId;
        enrollment.InstructorNote = note;
        await db.SaveChangesAsync();

        TempData["Status"] = $"Enrollment {status.ToString().ToLowerInvariant()}.";
        return RedirectToAction(nameof(Pending));
    }
}
