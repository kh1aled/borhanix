using System.ComponentModel.DataAnnotations;

namespace DepiLms.ViewModels
{
    public class ChangeEmailViewModel
    {
        public string CurrentEmail { get; set; } = string.Empty;


        [Required(ErrorMessage = "New email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new email.")]
        [EmailAddress]
        [Compare(nameof(NewEmail), ErrorMessage = "Emails do not match.")]
        public string ConfirmEmail { get; set; } = string.Empty;
    }
}