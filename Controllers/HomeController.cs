using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

public class HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        //get the total of instructors;
        var instructors = await userManager.GetUsersInRoleAsync(AppRoles.Instructor);
        ViewBag.CourseCount = await db.Courses.CountAsync(x => x.IsPublished);
        ViewBag.InstructorCount = instructors.Count.ToString();
        ViewBag.StudentCount = await db.Users.CountAsync();
        ViewBag.RoleNames = string.Join(" / ", AppRoles.Student, AppRoles.Instructor, AppRoles.Admin);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Courses(string? level ,string? search)
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


        var query = db.Courses
                    .Include(x => x.Modules)
                    .ThenInclude(x => x.Lessons)
                    .Include(x => x.Enrollments)
                    .Where(x => x.IsPublished || x.CreatedById == userId || User.IsInRole(AppRoles.Admin));

        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(x => x.Level == level);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => 
            x.Title.Contains(search.ToLower()) ||
            x.Code.Contains(search) || 
            x.Summary.Contains(search.ToLower()) ||
            x.Level.Contains(search.ToLower())
            );
        }

        var courses = await query
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

    [HttpGet]
    public async Task<IActionResult> Instructors(string? name)
    {
        var instructors = await db.Users
            .Include(x => x.InstructorProfile)
            .Where(x => x.InstructorProfile != null)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(name))
        {
            instructors = instructors
                .Where(x => 
                          x.FullName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                          x.InstructorProfile.Title.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                          x.InstructorProfile.Department.Contains(name, StringComparison.OrdinalIgnoreCase)
                          )
                .ToList();
        }

        var model = instructors.Select(x => new InstructorViewModel
        {
            Id = x.Id,
            FullName = x.FullName,
            Department = x.InstructorProfile!.Department,
            Bio = x.Bio,
            ProfilePhotoUrl = x.ProfilePhotoUrl,
            LevelOrTitle = x.InstructorProfile.Title,
            OfficeHours = x.InstructorProfile.OfficeHours,
            AvatarColor = x.AvatarColor,
            IsApproved = x.IsApproved
        }).ToList();

        return View(model);
    }

    [HttpGet("InstructorDetails/{id}")]
    public async Task<IActionResult> InstructorDetails(string id)
    {
        var instructor = await db.Users
            .Include(x => x.InstructorProfile)
            .FirstOrDefaultAsync(x => x.Id == id && x.InstructorProfile != null);

        if (instructor == null)
        {
            return NotFound();
        }

        var model = new InstructorProfileViewModel
        {
            Id = instructor.Id,
            FullName = instructor.FullName,
            Department = instructor.InstructorProfile!.Department,
            Bio = instructor.Bio,
            ProfilePhotoUrl = instructor.ProfilePhotoUrl,
            LevelOrTitle = instructor.InstructorProfile.Title,
            OfficeHours = instructor.InstructorProfile.OfficeHours,
            AvatarColor = instructor.AvatarColor,
            IsApproved = instructor.IsApproved
        };

        return View(model);
    }

    public IActionResult Error() => View();
}
