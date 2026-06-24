using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class ToolService : IToolService
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ToolService(IUnitOfWork uow, ICloudinaryService cloudinaryService, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _cloudinaryService = cloudinaryService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<ToolDto>> GetAllAsync(ToolFilterDto filter)
    {
        var (items, totalCount) = await _uow.Tools.GetFilteredAsync(
            filter.CategoryId, filter.Location, filter.MinPrice, filter.MaxPrice,
            filter.IsAvailable, filter.SearchTerm, filter.Page, filter.PageSize);

        return new PagedResult<ToolDto>
        {
            Items = items.Adapt<List<ToolDto>>(),
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ToolDto> GetByIdAsync(int id)
    {
        var tool = await _uow.Tools.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Tool {id} not found");

        return tool.Adapt<ToolDto>();
    }

    public async Task<ToolDto> CreateAsync(CreateToolDto dto, int ownerId)
    {
        var categoryCount = await _uow.Categories.CountAsync(c => c.Id == dto.CategoryId);
        if (categoryCount == 0)
            throw new KeyNotFoundException($"Category {dto.CategoryId} not found");

        var tool = dto.Adapt<Tool>();
        tool.OwnerId = ownerId;

        await _uow.Tools.AddAsync(tool);
        await _uow.SaveChangesAsync();

        return await GetByIdAsync(tool.Id);
    }

    public async Task<ToolDto> UpdateAsync(int id, UpdateToolDto dto, int ownerId)
    {
        var tool = await _uow.Tools.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tool {id} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        dto.Adapt(tool);

        await _uow.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id, int ownerId)
    {
        var tool = await _uow.Tools.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tool {id} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        _uow.Tools.Remove(tool);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<string>> UploadImagesAsync(int toolId, List<IFormFile> images, int ownerId)
    {
        var tool = await _uow.Tools.GetByIdAsync(toolId)
            ?? throw new KeyNotFoundException($"Tool {toolId} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        if (images == null || images.Count == 0)
            throw new ValidationException("No images provided");

        var urls = new List<string>();

        foreach (var img in images)
        {
            var uploadResult = await _cloudinaryService.UploadImageAsync(img);
            urls.Add(uploadResult.ImageUrl);

            await _uow.ToolImages.AddAsync(new ToolImage
            {
                ToolId = toolId,
                ImageUrl = uploadResult.ImageUrl,
                PublicId = uploadResult.PublicId
            });
        }

        await _uow.SaveChangesAsync();
        return urls;
    }

    public async Task DeleteImageAsync(int imageId, int ownerId)
    {
        var image = await _uow.ToolImages.GetByIdWithToolAsync(imageId)
            ?? throw new KeyNotFoundException($"Image {imageId} not found");

        if (image.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your image");

        if (!string.IsNullOrEmpty(image.PublicId))
        {
            await _cloudinaryService.DeleteImageAsync(image.PublicId);
        }

        _uow.ToolImages.Remove(image);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<ToolDto>> GetByOwnerAsync(int ownerId)
    {
        var tools = await _uow.Tools.GetByOwnerAsync(ownerId);
        return tools.Adapt<List<ToolDto>>();
    }
}
