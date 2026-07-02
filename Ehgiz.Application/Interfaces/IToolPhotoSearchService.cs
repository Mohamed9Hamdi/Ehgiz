using Ehgiz.Application.DTOs.Tools;
using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.Interfaces;

public interface IToolPhotoSearchService
{
    Task<PhotoSearchResultDto> SearchByPhotoAsync(
        IReadOnlyList<IFormFile> images,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
