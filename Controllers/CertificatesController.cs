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
public class CertificatesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICourseCompletionService completionService,
    ICertificateGenerationService certificateService) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Generate(int courseId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var user = await userManager.GetUserAsync(User);
        if (userId is null || user is null)
        {
            return Forbid();
        }

        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        if (!await completionService.IsEligibleForCertificateAsync(courseId, userId))
        {
            TempData["Error"] = "Complete all lessons and pass every quiz before generating your certificate.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        var status = await completionService.GetStatusAsync(courseId, userId);
        var certificate = await certificateService.GenerateAsync(
            course,
            user,
            status.AverageQuizPercent,
            cancellationToken);

        return RedirectToAction(nameof(Show), new { id = certificate.Id });
    }

    public async Task<IActionResult> Show(int id)
    {
        var certificate = await db.CourseCertificates
            .Include(x => x.Course)
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (certificate is null)
        {
            return NotFound();
        }

        if (!CanViewCertificate(certificate))
        {
            return Forbid();
        }

        return View(new CertificateViewModel
        {
            Certificate = certificate,
            StudentFullName = certificate.Student.FullName,
            CourseTitle = certificate.Course.Title,
            CourseCode = certificate.Course.Code
        });

        
    }

    private bool CanViewCertificate(CourseCertificate certificate)
    {
        var userId = userManager.GetUserId(User);
        if (User.IsInRole(AppRoles.Admin))
        {
            return true;
        }

        if (certificate.StudentId == userId)
        {
            return true;
        }

        return User.IsInRole(AppRoles.Instructor) && certificate.Course.CreatedById == userId;
    }
}
