namespace DepiLms.Services;

public interface IVideoDurationService
{
    int? TryGetDurationSeconds(string? webRootPath, string? videoUrl, int fallbackDurationMinutes, int? clientReportedSeconds = null);
    Task<int?> TryGetUploadDurationSecondsAsync(IFormFile file, string savedRelativePath, int fallbackDurationMinutes);
}
