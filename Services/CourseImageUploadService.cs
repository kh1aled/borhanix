namespace DepiLms.Services;

public class CourseImageUploadService(IWebHostEnvironment environment) : ICourseImageUploadService
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public async Task<string?> SaveCoverPhotoAsync(IFormFile? file, string? existingRelativePath = null)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!TryValidateImageSignature(file, out var extension))
        {
            throw new InvalidOperationException("Cover photo must be a valid JPG or PNG image.");
        }

        var folder = Path.Combine(environment.WebRootPath, "uploads", "courses");
        Directory.CreateDirectory(folder);

        if (!string.IsNullOrWhiteSpace(existingRelativePath))
        {
            DeleteIfLocal(existingRelativePath);
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/courses/{fileName}";
    }

    internal static bool TryValidateImageSignature(IFormFile file, out string extension)
    {
        extension = string.Empty;
        if (file.Length < 4)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 4)
        {
            return false;
        }

        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            extension = ".png";
            return true;
        }

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            extension = ".jpg";
            return true;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return false;
        }

        return false;
    }

    private void DeleteIfLocal(string relativePath)
    {
        if (!relativePath.StartsWith("/uploads/courses/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fullPath = Path.Combine(environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
