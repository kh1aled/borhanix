using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DepiLms.ViewModels;

public class ProfileViewModel
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Phone, MaxLength(40)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(120)]
    public string ProgramOrDepartment { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? LevelOrTitle { get; set; }

    [MaxLength(120)]
    public string? EmergencyContactOrOfficeHours { get; set; }

    [MaxLength(16)]
    public string AvatarColor { get; set; } = "#2563eb";

    public string? ProfilePhotoUrl { get; set; }
    public IFormFile? ProfilePhoto { get; set; }
    public bool IsStudent { get; set; }
    public bool IsInstructor { get; set; }
    public bool IsApproved { get; set; }
}
