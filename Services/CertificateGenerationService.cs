using DepiLms.Data;
using DepiLms.Models;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Services;

public class CertificateGenerationService(
    ApplicationDbContext db,
    IOpenRouterAiService aiService) : ICertificateGenerationService
{
    public async Task<CourseCertificate> GenerateAsync(
        Course course,
        ApplicationUser student,
        decimal finalGradePercent,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CourseCertificates
            .FirstOrDefaultAsync(x => x.CourseId == course.Id && x.StudentId == student.Id, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var prompt = $"""
            Write a unique, highly personalized congratulatory certificate message (2-3 sentences) for {student.FullName}
            who completed the course "{course.Title}" with a final grade of {finalGradePercent:F1}%.
            Highlight their dedication and one specialized skill they demonstrated. Keep it formal yet motivational.
            Do not include placeholders or markdown.
            """;

        var aiResult = await aiService.AskAsync(
            [],
            prompt,
            $"Course category: {course.Category}. Level: {course.Level}.",
            cancellationToken);

        var certificate = new CourseCertificate
        {
            CourseId = course.Id,
            StudentId = student.Id,
            CertificateNumber = $"CERT-{course.Id}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            FinalGradePercent = finalGradePercent,
            AiPersonalMessage = aiResult.Content.Trim(),
            IssuedAt = DateTimeOffset.UtcNow
        };

        db.CourseCertificates.Add(certificate);
        await db.SaveChangesAsync(cancellationToken);
        return certificate;
    }
}
