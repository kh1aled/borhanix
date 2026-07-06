using DepiLms.Models;
using System.ComponentModel.DataAnnotations;

namespace DepiLms.ViewModels
{
    public class CoursesViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = "Technology";
        public string Level { get; set; } = "Beginner";
        public string? HeroImageUrl { get; set; }
        public string? CoverPhotoPath { get; set; }

        public string AccentColor { get; set; } = "#14b8a6";

        public bool IsPublished { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string CreatedById { get; set; } = string.Empty;
        public ApplicationUser CreatedBy { get; set; } = default!;
    }
}
