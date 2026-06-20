using System.ComponentModel.DataAnnotations;

namespace DepiLms.ViewModels;

public class AdminUserRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Roles { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }
}

public class AdminUsersViewModel
{
    public IReadOnlyCollection<AdminUserRowViewModel> Users { get; set; } = [];
}

public class AdminUserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsApproved { get; set; }
}
