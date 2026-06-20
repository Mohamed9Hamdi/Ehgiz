using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.Interfaces;

public interface IToolService
{

 
    Task<PagedResult<ToolDto>> GetAllAsync(ToolFilterDto filter);

  

    Task<ToolDto> GetByIdAsync(int id);

 
    Task<ToolDto> CreateAsync(CreateToolDto dto, int ownerId);

  
    Task<ToolDto> UpdateAsync(int id, UpdateToolDto dto, int ownerId);

  
    Task DeleteAsync(int id, int ownerId);

  
    Task<List<string>> UploadImagesAsync(int toolId, List<IFormFile> images, int ownerId);

 
    Task DeleteImageAsync(int imageId, int ownerId);

 
    Task<List<ToolDto>> GetByOwnerAsync(int ownerId);
}