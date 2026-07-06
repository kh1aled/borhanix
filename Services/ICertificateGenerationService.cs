using DepiLms.Models;

namespace DepiLms.Services;

public interface ICertificateGenerationService
{
    Task<CourseCertificate> GenerateAsync(
        Course course,
        ApplicationUser student,
        decimal finalGradePercent,
        CancellationToken cancellationToken = default);
}
