using System.ComponentModel.DataAnnotations;
using DepiLms.Models;
using Microsoft.AspNetCore.Http;

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
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int EnrollmentCount { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
}

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = default!;
    public Enrollment? Enrollment { get; set; }
    public bool CanRequestEnrollment { get; set; }
    public bool CanManage { get; set; }
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

    [Required, MaxLength(16)]
    public string AccentColor { get; set; } = "#14b8a6";

    public bool IsPublished { get; set; } = true;
}

public class ModuleCreateViewModel
{
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
}

public class AssignmentCreateViewModel
{
    public int CourseId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Brief { get; set; } = string.Empty;

    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public decimal MaxPoints { get; set; } = 100;
}
