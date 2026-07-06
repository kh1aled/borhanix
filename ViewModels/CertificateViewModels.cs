using DepiLms.Models;
using DepiLms.Services;

namespace DepiLms.ViewModels;

public class CertificateViewModel
{
    public CourseCertificate Certificate { get; set; } = default!;
    public string StudentFullName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
}
