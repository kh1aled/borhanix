using DepiLms.Models;

namespace DepiLms.Services;

public interface IQrCodeService
{
    string BuildStudentPayload(StudentProfile profile);
    string? ExtractStudentToken(string payload);
    string CreateSvg(string payload);
    string CreatePngDataUri(string payload);
}
