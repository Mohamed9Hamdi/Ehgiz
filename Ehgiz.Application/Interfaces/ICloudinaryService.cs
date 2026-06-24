using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.Interfaces;

public interface ICloudinaryService
{
    Task<ImageUploadResult> UploadImageAsync(IFormFile file);
    Task<bool> DeleteImageAsync(string publicId);
}

public class ImageUploadResult
{
    public string ImageUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}
