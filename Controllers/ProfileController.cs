using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize]
public class ProfileController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildModelAsync(user);
        return View(model);
    }

    public async Task<IActionResult> Edit()
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildModelAsync(user);
        return View(model);
    }



    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateProfileViewModel model)
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Bio = model.Bio;
        user.AvatarColor = model.AvatarColor;

        var photoUrl = await SaveProfilePhotoAsync(model.ProfilePhoto);
        if (!ModelState.IsValid)
        {
            model.ProfilePhotoUrl = user.ProfilePhotoUrl;
            return View("Index", model);
        }

        if (photoUrl is not null)
        {
            user.ProfilePhotoUrl = photoUrl;
        }

        if (user.StudentProfile is not null)
        {
            user.StudentProfile.Program = model.ProgramOrDepartment;
            user.StudentProfile.Level = model.LevelOrTitle ?? user.StudentProfile.Level;
            user.StudentProfile.EmergencyContact = model.EmergencyContactOrOfficeHours;
            user.StudentProfile.PhotoUrl = user.ProfilePhotoUrl;
        }

        if (user.InstructorProfile is not null)
        {
            user.InstructorProfile.Department = model.ProgramOrDepartment;
            user.InstructorProfile.Title = model.LevelOrTitle ?? user.InstructorProfile.Title;
            user.InstructorProfile.OfficeHours = model.EmergencyContactOrOfficeHours;
        }

        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync();
        TempData["ToastSuccess"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> LoadCurrentUserAsync()
    {
        var userId = userManager.GetUserId(User);
        return await db.Users
            .Include(x => x.StudentProfile)
            .Include(x => x.InstructorProfile)
            .FirstOrDefaultAsync(x => x.Id == userId);
    }

    private static async Task<ProfileViewModel> BuildModelAsync(ApplicationUser user)
    {
        await Task.CompletedTask;
        return new ProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Bio = user.Bio,
            Email = user.Email,
            AvatarColor = user.AvatarColor,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            IsApproved = user.IsApproved,
            IsStudent = user.StudentProfile is not null,
            IsInstructor = user.InstructorProfile is not null,
            ProgramOrDepartment = user.StudentProfile?.Program ?? user.InstructorProfile?.Department ?? "",
            LevelOrTitle = user.StudentProfile?.Level ?? user.InstructorProfile?.Title,
            EmergencyContactOrOfficeHours = user.StudentProfile?.EmergencyContact ?? user.InstructorProfile?.OfficeHours
        };
    }
    private async Task<string?> SaveProfilePhotoAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError(nameof(ProfileViewModel.ProfilePhoto), "Use JPG, PNG, or WEBP.");
            return null;
        }

        var folder = Path.Combine(environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/profiles/{fileName}";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(ChangePasswordViewModel model)
    {
        var user = await LoadCurrentUserAsync();

        if (user == null)
            return Challenge();

        var profileModel = await BuildModelAsync(user);


        if (!ModelState.IsValid)
        {
            TempData["PasswordErrors"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToArray();

            return View("Edit", profileModel);
        }

        if (user is null)
            return Challenge();

        var result = await userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!result.Succeeded)
        {
            TempData["PasswordErrors"] = result.Errors
                .Select(e => e.Description)
                .ToArray();

            return View("Edit", profileModel);
        }

        TempData["ToastSuccess"] = "Password updated successfully.";
        return View("Edit", profileModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(ChangeEmailViewModel model)
    {
        var user = await LoadCurrentUserAsync();

        if (user == null)
            return Challenge();

        var profileModel = await BuildModelAsync(user);

        if (!ModelState.IsValid)
            return View("Edit", profileModel);


        if (user is null)
            return Challenge();

        var token = await userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);

        var result = await userManager.ChangeEmailAsync(user, model.NewEmail, token);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("Edit", profileModel);
        }

        await userManager.SetUserNameAsync(user, model.NewEmail);

        user.IsApproved = false;

        await userManager.UpdateAsync(user);

        await signInManager.SignOutAsync();

        TempData["ToastSuccess"] =
            "Email updated successfully. Please login again.";

        return RedirectToAction("Login", "Account");
    }
}
