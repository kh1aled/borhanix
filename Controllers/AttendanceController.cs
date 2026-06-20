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
public class AttendanceController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IQrCodeService qrCodeService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);

        if (User.IsInRole(AppRoles.Student))
        {
            var records = await db.AttendanceRecords
                .Include(x => x.AttendanceSession)
                .ThenInclude(x => x.Course)
                .Where(x => x.StudentId == userId)
                .OrderByDescending(x => x.CapturedAt)
                .ToListAsync();

            return View(new AttendanceIndexViewModel { Records = records });
        }

        var courses = await db.Courses
            .Where(x => User.IsInRole(AppRoles.Admin) || x.CreatedById == userId)
            .OrderBy(x => x.Title)
            .ToListAsync();

        var courseIds = courses.Select(x => x.Id).ToList();
        var sessions = await db.AttendanceSessions
            .Include(x => x.Course)
            .Include(x => x.Records)
            .Where(x => courseIds.Contains(x.CourseId))
            .OrderByDescending(x => x.SessionDate)
            .ToListAsync();

        return View(new AttendanceIndexViewModel
        {
            Courses = courses,
            Sessions = sessions,
            NewSession = new AttendanceSessionCreateViewModel
            {
                CourseId = courses.FirstOrDefault()?.Id ?? 0
            }
        });
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession(AttendanceSessionCreateViewModel model)
    {
        var course = await db.Courses.FindAsync(model.CourseId);
        if (course is null)
        {
            return NotFound();
        }

        var userId = userManager.GetUserId(User);
        if (!User.IsInRole(AppRoles.Admin) && course.CreatedById != userId)
        {
            return Forbid();
        }

        if (ModelState.IsValid && userId is not null)
        {
            db.AttendanceSessions.Add(new AttendanceSession
            {
                CourseId = model.CourseId,
                Title = model.Title,
                SessionDate = model.SessionDate,
                OpensAt = model.OpensAt,
                ClosesAt = model.ClosesAt,
                CreatedById = userId
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Scanner(int? sessionId)
    {
        var userId = userManager.GetUserId(User);
        var sessions = await db.AttendanceSessions
            .Include(x => x.Course)
            .Where(x => User.IsInRole(AppRoles.Admin) || x.Course.CreatedById == userId)
            .OrderByDescending(x => x.SessionDate)
            .ToListAsync();

        var selected = sessionId is null
            ? sessions.FirstOrDefault()
            : sessions.FirstOrDefault(x => x.Id == sessionId);

        return View(new AttendanceScanViewModel
        {
            SessionId = selected?.Id ?? 0,
            Session = selected,
            AvailableSessions = sessions
        });
    }

    [Authorize(Roles = $"{AppRoles.Instructor},{AppRoles.Admin}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(AttendanceScanViewModel model)
    {
        var session = await db.AttendanceSessions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == model.SessionId);

        if (session is null)
        {
            return NotFound();
        }

        var userId = userManager.GetUserId(User);
        if (!User.IsInRole(AppRoles.Admin) && session.Course.CreatedById != userId)
        {
            return Forbid();
        }

        var token = qrCodeService.ExtractStudentToken(model.Payload);
        var profile = token is null
            ? null
            : await db.StudentProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.QrToken == token);

        if (profile is null)
        {
            TempData["Error"] = "QR payload was not recognized as a student ID.";
            return RedirectToAction(nameof(Scanner), new { sessionId = model.SessionId });
        }

        var approved = await db.Enrollments.AnyAsync(x =>
            x.CourseId == session.CourseId &&
            x.StudentId == profile.ApplicationUserId &&
            x.Status == EnrollmentStatus.Approved);

        if (!approved)
        {
            TempData["Error"] = $"{profile.User.FullName} is not approved for this course.";
            return RedirectToAction(nameof(Scanner), new { sessionId = model.SessionId });
        }

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(x => x.AttendanceSessionId == session.Id && x.StudentId == profile.ApplicationUserId);

        if (record is null)
        {
            db.AttendanceRecords.Add(new AttendanceRecord
            {
                AttendanceSessionId = session.Id,
                StudentId = profile.ApplicationUserId,
                Status = DateTimeOffset.UtcNow > session.OpensAt.AddMinutes(20) ? AttendanceStatus.Late : AttendanceStatus.Present,
                Source = AttendanceSource.IdCard,
                DeviceInfo = Request.Headers["User-Agent"].ToString()
            });
        }
        else
        {
            record.CapturedAt = DateTimeOffset.UtcNow;
            record.Source = AttendanceSource.IdCard;
            record.DeviceInfo = Request.Headers["User-Agent"].ToString();
        }

        await db.SaveChangesAsync();
        TempData["Status"] = $"{profile.User.FullName} marked present for {session.Title}.";
        return RedirectToAction(nameof(Scanner), new { sessionId = model.SessionId });
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Admin}")]
    public async Task<IActionResult> StudentCard(string? userId = null)
    {
        var targetUserId = User.IsInRole(AppRoles.Admin) && !string.IsNullOrWhiteSpace(userId)
            ? userId
            : userManager.GetUserId(User);

        var profile = await db.StudentProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.ApplicationUserId == targetUserId);

        if (profile is null)
        {
            return NotFound();
        }

        var payload = qrCodeService.BuildStudentPayload(profile);
        var records = await db.AttendanceRecords.CountAsync(x => x.StudentId == profile.ApplicationUserId);
        var sessions = await db.AttendanceSessions
            .CountAsync(x => x.Course.Enrollments.Any(e => e.StudentId == profile.ApplicationUserId && e.Status == EnrollmentStatus.Approved));

        return View(new StudentCardViewModel
        {
            User = profile.User,
            Profile = profile,
            QrPayload = payload,
            QrSvg = qrCodeService.CreateSvg(payload),
            QrImageDataUri = qrCodeService.CreatePngDataUri(payload),
            ApprovedCourses = await db.Enrollments.CountAsync(x => x.StudentId == profile.ApplicationUserId && x.Status == EnrollmentStatus.Approved),
            AttendanceRate = sessions == 0 ? 0 : Math.Round((decimal)records / sessions * 100, 1)
        });
    }
}
