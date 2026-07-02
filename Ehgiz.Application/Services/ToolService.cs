using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class ToolService : IToolService
{
    private readonly IUnitOfWork _uow;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ToolService(IUnitOfWork uow, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<ToolDto>> GetAllAsync(ToolFilterDto filter)
    {
        var query = _uow.Tools.Query();

        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId);

        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(t => t.Location!.Contains(filter.Location));

        if (filter.MinPrice.HasValue)
            query = query.Where(t => t.PricePerDay >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(t => t.PricePerDay <= filter.MaxPrice);

        if (filter.IsAvailable.HasValue)
            query = query.Where(t => t.IsAvailable == filter.IsAvailable);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(t =>
                t.Name.Contains(filter.SearchTerm) ||
                (t.Description != null && t.Description.Contains(filter.SearchTerm)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ProjectToType<ToolDto>()
            .ToListAsync();

        return new PagedResult<ToolDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ToolDto> GetByIdAsync(int id)
    {
        return await _uow.Tools.Query()
            .Where(t => t.Id == id)
            .ProjectToType<ToolDto>()
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Tool {id} not found");
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

        var uploadPath = Path.Combine(_env.ContentRootPath, "uploads", "tools", toolId.ToString());
        Directory.CreateDirectory(uploadPath);

        if (_httpContextAccessor.HttpContext is null)
            throw new InvalidOperationException("No HTTP context available.");

        var request = _httpContextAccessor.HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";

        var urls = new List<string>();

        foreach (var img in images)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await img.CopyToAsync(stream);

            var url = $"{baseUrl}/uploads/tools/{toolId}/{fileName}";
            urls.Add(url);

            await _uow.ToolImages.AddAsync(new ToolImage
            {
                ToolId = toolId,
                ImageUrl = url
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

        var parts = image.ImageUrl.Split("/uploads/");
        if (parts.Length == 2)
        {
            var filePath = Path.Combine(
                _env.ContentRootPath,
                "uploads",
                parts[1].Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        _uow.ToolImages.Remove(image);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<ToolDto>> GetByOwnerAsync(int ownerId)
    {
        return await _uow.Tools.Query()
            .Where(t => t.OwnerId == ownerId)
            .ProjectToType<ToolDto>()
            .ToListAsync();
    }
}
