using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DepiLms.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var loginUser = await userManager.FindByEmailAsync(model.Email);
        if (loginUser is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!loginUser.IsApproved)
        {
            ModelState.AddModelError(string.Empty, "Your account is waiting for admin approval.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(loginUser, model.Password, model.RememberMe, false);
        if (result.Succeeded)
        {
            loginUser.LastActiveAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(loginUser);

            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role is not AppRoles.Student and not AppRoles.Instructor)
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Student or Instructor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            IsApproved = false,
            FullName = model.FullName,
            AvatarColor = model.Role == AppRoles.Instructor ? "#7c3aed" : "#0f766e"
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, model.Role);

        if (model.Role == AppRoles.Student)
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                ApplicationUserId = user.Id,
                StudentCode = $"DEPI-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Program = model.ProgramOrDepartment,
                Level = "Applicant"
            });
        }
        else
        {
            db.InstructorProfiles.Add(new InstructorProfile
            {
                ApplicationUserId = user.Id,
                Department = model.ProgramOrDepartment,
                Title = "Instructor"
            });
        }

        await db.SaveChangesAsync();
        TempData["Status"] = "Account created. Admin approval is required before login.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied() => View();
}
