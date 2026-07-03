using Ehgiz.Application.DTOs.Tools;
using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.Interfaces;

public interface IToolSuggestionService
{
    Task<ToolSuggestionDto> SuggestFromImagesAsync(IReadOnlyList<IFormFile> images, CancellationToken cancellationToken = default);
}
