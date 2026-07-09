using DepiLms.Data;
using DepiLms.Models;
using DepiLms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
        => View(await BuildCartAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int courseId, string? returnUrl)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var alreadyEnrolled = await db.Enrollments.AnyAsync(x =>
            x.CourseId == courseId &&
            x.StudentId == userId &&
            x.Status == EnrollmentStatus.Approved);

        var exists = await db.CartItems.AnyAsync(x => x.CourseId == courseId && x.StudentId == userId);
        var courseExists = await db.Courses.AnyAsync(x => x.Id == courseId && x.IsPublished);

        if (!alreadyEnrolled && !exists && courseExists)
        {
            db.CartItems.Add(new CartItem { CourseId = courseId, StudentId = userId });
            await db.SaveChangesAsync();
            TempData["ToastSuccess"] = "Course added to your cart.";
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = userManager.GetUserId(User);
        var item = await db.CartItems.FirstOrDefaultAsync(x => x.Id == id && x.StudentId == userId);
        if (item is not null)
        {
            db.CartItems.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var userId = userManager.GetUserId(User);
        await db.CartItems.Where(x => x.StudentId == userId).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await BuildCartAsync();
        return View(new SandboxCheckoutViewModel
        {
            Items = cart.Items,
            Total = cart.Total,
            Currency = cart.Currency,
            CardholderName = User.Identity?.Name ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(SandboxCheckoutViewModel model)
    {
        var cart = await BuildCartAsync();
        model.Items = cart.Items;
        model.Total = cart.Total;
        model.Currency = cart.Currency;

        if (!cart.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
        }

        var cardNumber = DigitsOnly(model.CardNumber);
        if (!IsSandboxCardAccepted(cardNumber))
        {
            ModelState.AddModelError(nameof(model.CardNumber), "Use a sandbox success card such as 4242 4242 4242 4242. 4000 0000 0000 0002 simulates a decline.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        foreach (var item in cart.Items)
        {
            var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.CourseId == item.CourseId && x.StudentId == userId);
            if (enrollment is null)
            {
                db.Enrollments.Add(new Enrollment
                {
                    CourseId = item.CourseId,
                    StudentId = userId,
                    Status = EnrollmentStatus.Approved,
                    ReviewedAt = DateTimeOffset.UtcNow,
                    InstructorNote = "Approved by sandbox checkout."
                });
            }
            else
            {
                enrollment.Status = EnrollmentStatus.Approved;
                enrollment.ReviewedAt = DateTimeOffset.UtcNow;
                enrollment.InstructorNote = "Approved by sandbox checkout.";
            }
        }

        db.SandboxPayments.Add(new SandboxPayment
        {
            StudentId = userId,
            Amount = cart.Total,
            Currency = cart.Currency,
            CardLast4 = cardNumber[^4..],
            Reference = $"TEST-{Guid.NewGuid():N}"[..18],
            Status = SandboxPaymentStatus.Succeeded
        });

        await db.CartItems.Where(x => x.StudentId == userId).ExecuteDeleteAsync();
        await db.SaveChangesAsync();

        TempData["ToastSuccess"] = "Sandbox payment succeeded. Your courses are ready.";
        return RedirectToAction(nameof(Success));
    }

    public IActionResult Success() => View();

    private async Task<CartViewModel> BuildCartAsync()
    {
        var userId = userManager.GetUserId(User);
        var items = await db.CartItems
            .Include(x => x.Course)
            .Where(x => x.StudentId == userId)
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync();

        var itemModels = items.Select(x => new CartItemViewModel
        {
            Id = x.Id,
            CourseId = x.CourseId,
            Title = x.Course.Title,
            Code = x.Course.Code,
            Summary = x.Course.Summary,
            CoverImageUrl = CourseMedia.ResolveCoverImage(x.Course),
            Price = x.Course.Price,
            Currency = x.Course.Currency
        }).ToList();

        return new CartViewModel
        {
            Items = itemModels,
            Total = itemModels.Sum(x => x.Price),
            Currency = itemModels.FirstOrDefault()?.Currency ?? "USD"
        };
    }

    private IActionResult RedirectBack(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));

    private static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());

    private static bool IsSandboxCardAccepted(string cardNumber)
        => cardNumber is "4242424242424242" or "5555555555554444" or "4111111111111111";
}
