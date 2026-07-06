namespace DepiLms.Services;

public interface ICourseImageUploadService
{
    Task<string?> SaveCoverPhotoAsync(IFormFile? file, string? existingRelativePath = null);
}
