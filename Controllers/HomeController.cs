using DepiLms.Data;
using DepiLms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.CourseCount = await db.Courses.CountAsync(x => x.IsPublished);
        ViewBag.StudentCount = await db.Users.CountAsync();
        ViewBag.RoleNames = string.Join(" / ", AppRoles.Student, AppRoles.Instructor, AppRoles.Admin);
        return View();
    }

    public IActionResult Error() => View();
}
