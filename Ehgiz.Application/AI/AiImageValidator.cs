using Ehgiz.Application.Settings;
using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.AI;

public static class AiImageValidator
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public static void Validate(IReadOnlyList<IFormFile> images, GitHubModelsSettings settings)
    {
        if (images is null || images.Count == 0)
            throw new InvalidOperationException("At least one image is required.");

        if (images.Count > settings.MaxImages)
            throw new InvalidOperationException($"A maximum of {settings.MaxImages} images is allowed.");

        foreach (var image in images)
        {
            if (image.Length == 0)
                throw new InvalidOperationException("One or more image files are empty.");

            if (image.Length > settings.MaxImageBytes)
                throw new InvalidOperationException("One or more images exceed the 5 MB size limit.");

            if (string.IsNullOrWhiteSpace(image.ContentType) || !AllowedContentTypes.Contains(image.ContentType))
                throw new InvalidOperationException("Only JPEG, PNG, WEBP, and GIF images are supported.");
        }
    }
}
