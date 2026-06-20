using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Common;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class ToolService : IToolService
{
    private readonly EhgizDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ToolService(EhgizDbContext context, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<ToolDto>> GetAllAsync(ToolFilterDto filter)
    {
        var query = _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .AsQueryable();

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

        var tools = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ToolDto>
        {
            Items = tools.Adapt<List<ToolDto>>(),
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ToolDto> GetByIdAsync(int id)
    {
        var tool = await _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tool == null)
            throw new KeyNotFoundException($"Tool {id} not found");

        return tool.Adapt<ToolDto>();
    }

    public async Task<ToolDto> CreateAsync(CreateToolDto dto, int ownerId)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
            throw new KeyNotFoundException($"Category {dto.CategoryId} not found");

        var tool = dto.Adapt<Tool>();
        tool.OwnerId = ownerId;

        _context.Tools.Add(tool);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(tool.Id);
    }

    public async Task<ToolDto> UpdateAsync(int id, UpdateToolDto dto, int ownerId)
    {
        var tool = await _context.Tools.FindAsync(id);

        if (tool == null)
            throw new KeyNotFoundException($"Tool {id} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        dto.Adapt(tool);

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id, int ownerId)
    {
        var tool = await _context.Tools.FindAsync(id);

        if (tool == null)
            throw new KeyNotFoundException($"Tool {id} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        _context.Tools.Remove(tool);
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> UploadImagesAsync(int toolId, List<IFormFile> images, int ownerId)
    {
        var tool = await _context.Tools.FindAsync(toolId);

        if (tool == null)
            throw new KeyNotFoundException($"Tool {toolId} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        if (images == null || images.Count == 0)
            throw new ValidationException("No images provided");

        var uploadPath = Path.Combine(_env.ContentRootPath, "uploads", "tools", toolId.ToString());
        Directory.CreateDirectory(uploadPath);

        var request = _httpContextAccessor.HttpContext!.Request;
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

            _context.ToolImages.Add(new ToolImage
            {
                ToolId = toolId,
                ImageUrl = url
            });
        }

        await _context.SaveChangesAsync();
        return urls;
    }

    public async Task DeleteImageAsync(int imageId, int ownerId)
    {
        var image = await _context.ToolImages
            .Include(i => i.Tool)
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (image == null)
            throw new KeyNotFoundException($"Image {imageId} not found");

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

        _context.ToolImages.Remove(image);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ToolDto>> GetByOwnerAsync(int ownerId)
    {
        var tools = await _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId)
            .ToListAsync();

        return tools.Adapt<List<ToolDto>>();
    }
}