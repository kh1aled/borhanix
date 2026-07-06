using System.ComponentModel.DataAnnotations;
using DepiLms.Models;
using DepiLms.Services;

namespace DepiLms.ViewModels;

public class CourseCardViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#14b8a6";
    public string? HeroImageUrl { get; set; }
    public string? CoverPhotoPath { get; set; }
    public string? CoverImageUrl => !string.IsNullOrWhiteSpace(CoverPhotoPath) ? CoverPhotoPath : HeroImageUrl;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string PriceLabel => Price <= 0 ? "Free" : $"{Currency} {Price:N2}";
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int EnrollmentCount { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public bool IsInCart { get; set; }
    public bool IsSaved { get; set; }
}

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = default!;
    public Enrollment? Enrollment { get; set; }
    public bool CanRequestEnrollment { get; set; }
    public bool IsInCart { get; set; }
    public bool IsSaved { get; set; }
    public bool CanManage { get; set; }
    public IReadOnlyDictionary<int, int> QuizAttemptIds { get; set; } = new Dictionary<int, int>();
    public IReadOnlySet<int> CompletedLessonIds { get; set; } = new HashSet<int>();
    public IReadOnlyDictionary<int, LessonProgressInfo> LessonProgressMap { get; set; } = new Dictionary<int, LessonProgressInfo>();
    public CourseCompletionStatus? CompletionStatus { get; set; }
    public bool CanGenerateCertificate { get; set; }
    public int? CertificateId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}

public class CourseEditViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(24)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Category { get; set; } = "Technology";

    [Required, MaxLength(80)]
    public string Level { get; set; } = "Beginner";

    [MaxLength(180)]
    public string? HeroImageUrl { get; set; }

    public string? CoverPhotoPath { get; set; }

    public IFormFile? CoverPhoto { get; set; }

    [Required, MaxLength(16)]
    public string AccentColor { get; set; } = "#14b8a6";

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public bool IsPublished { get; set; } = true;
}

public class CartItemViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string PriceLabel => Price <= 0 ? "Free" : $"{Currency} {Price:N2}";
}

public class CartViewModel
{
    public IReadOnlyCollection<CartItemViewModel> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string TotalLabel => Total <= 0 ? "Free" : $"{Currency} {Total:N2}";
}

public class SandboxCheckoutViewModel : CartViewModel
{
    [Required, MaxLength(80)]
    public string CardholderName { get; set; } = string.Empty;

    [Required, Display(Name = "Card number")]
    public string CardNumber { get; set; } = "4242 4242 4242 4242";

    [Required, Display(Name = "Expiry")]
    public string Expiry { get; set; } = "12/34";

    [Required, MaxLength(4)]
    public string Cvc { get; set; } = "123";
}

public class ModuleCreateViewModel
{
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Summary { get; set; }
}

public class ModuleEditViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Summary { get; set; }
}

public class LessonCreateViewModel
{
    public int ModuleId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? VideoUrl { get; set; }

    [MaxLength(240)]
    public string? ResourceUrl { get; set; }

    public IFormFile? VideoFile { get; set; }

    public int DurationMinutes { get; set; } = 30;
    public int? VideoDurationSeconds { get; set; }
}

public class LessonProgressInfo
{
    public int MaxWatchedSeconds { get; set; }
    public decimal ViewingPercent { get; set; }
    public bool IsComplete { get; set; }
}

public class LessonEditViewModel
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? VideoUrl { get; set; }

    [MaxLength(240)]
    public string? ResourceUrl { get; set; }

    public IFormFile? VideoFile { get; set; }

    public int DurationMinutes { get; set; } = 30;
    public int? VideoDurationSeconds { get; set; }
}

public class AssignmentCreateViewModel
{
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Brief { get; set; } = string.Empty;

    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public int DeadlineHours { get; set; } = 48;
    public decimal MaxPoints { get; set; } = 100;
}
