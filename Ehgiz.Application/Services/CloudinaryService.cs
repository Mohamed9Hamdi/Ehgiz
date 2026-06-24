using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> config)
    {
        var acc = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );

        _cloudinary = new Cloudinary(acc);
    }

    public async Task<Interfaces.ImageUploadResult> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream)
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return new Interfaces.ImageUploadResult
        {
            ImageUrl = result.SecureUrl.ToString(),
            PublicId = result.PublicId
        };
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }
}
