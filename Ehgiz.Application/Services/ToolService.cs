using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class ToolService : IToolService
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ISavedSearchService _savedSearchService;
    private readonly ILogger<ToolService> _logger;

    public ToolService(
        IUnitOfWork uow,
        ICloudinaryService cloudinaryService,
        ISavedSearchService savedSearchService,
        ILogger<ToolService> logger)
    {
        _uow = uow;
        _cloudinaryService = cloudinaryService;
        _savedSearchService = savedSearchService;
        _logger = logger;
    }

    public async Task<PagedResult<ToolDto>> GetAllAsync(ToolFilterDto filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _uow.Tools.Query();

        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId);

        if (filter.Condition.HasValue)
            query = query.Where(t => t.Condition == filter.Condition);

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

        var nearSearch = filter.NearLat.HasValue && filter.NearLng.HasValue;
        if (nearSearch)
        {
            var lat = filter.NearLat!.Value;
            var lng = filter.NearLng!.Value;

            query = query.Where(t => t.Latitude != null && t.Longitude != null);

            if (filter.RadiusKm.HasValue)
            {
                var radiusKm = filter.RadiusKm.Value;
                query = query.Where(t =>
                    EarthDiameterKm * Math.Asin(Math.Sqrt(
                        Math.Pow(Math.Sin((t.Latitude!.Value - lat) * Math.PI / 360.0), 2) +
                        Math.Cos(lat * Math.PI / 180.0) * Math.Cos(t.Latitude.Value * Math.PI / 180.0) *
                        Math.Pow(Math.Sin((t.Longitude!.Value - lng) * Math.PI / 360.0), 2))) <= radiusKm);
            }

            query = query.OrderBy(t =>
                EarthDiameterKm * Math.Asin(Math.Sqrt(
                    Math.Pow(Math.Sin((t.Latitude!.Value - lat) * Math.PI / 360.0), 2) +
                    Math.Cos(lat * Math.PI / 180.0) * Math.Cos(t.Latitude.Value * Math.PI / 180.0) *
                    Math.Pow(Math.Sin((t.Longitude!.Value - lng) * Math.PI / 360.0), 2))));
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ProjectToType<ToolDto>()
            .ToListAsync();

        if (nearSearch)
        {
            foreach (var item in items.Where(i => i.Latitude.HasValue && i.Longitude.HasValue))
            {
                item.DistanceKm = Math.Round(HaversineKm(
                    filter.NearLat!.Value, filter.NearLng!.Value,
                    item.Latitude!.Value, item.Longitude!.Value), 2);
            }
        }

        return new PagedResult<ToolDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    private const double EarthDiameterKm = 12742.0;

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        return EarthDiameterKm * Math.Asin(Math.Sqrt(
            Math.Pow(Math.Sin((lat2 - lat1) * Math.PI / 360.0), 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Pow(Math.Sin((lng2 - lng1) * Math.PI / 360.0), 2)));
    }

    private static void ValidateCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
            throw new ValidationException("Latitude and longitude must be provided together.");
    }

    // Defense in depth alongside the DTO [Range] attributes: a negative price or
    // insurance would make booking totals negative and credit the renter's wallet.
    private static void ValidatePricing(decimal pricePerDay, decimal insurancePrice)
    {
        if (pricePerDay <= 0)
            throw new ValidationException("Price per day must be greater than zero.");

        if (insurancePrice < 0)
            throw new ValidationException("Insurance price cannot be negative.");
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

        ValidateCoordinates(dto.Latitude, dto.Longitude);
        ValidatePricing(dto.PricePerDay, dto.InsurancePrice);

        var tool = dto.Adapt<Tool>();
        tool.OwnerId = ownerId;

        await _uow.Tools.AddAsync(tool);
        await _uow.SaveChangesAsync();

        try
        {
            await _savedSearchService.NotifyMatchesAsync(tool.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify saved-search matches for tool {ToolId}", tool.Id);
        }

        return await GetByIdAsync(tool.Id);
    }

    public async Task<ToolDto> UpdateAsync(int id, UpdateToolDto dto, int ownerId)
    {
        var tool = await _uow.Tools.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tool {id} not found");

        if (tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your tool");

        ValidateCoordinates(dto.Latitude, dto.Longitude);
        ValidatePricing(dto.PricePerDay, dto.InsurancePrice);

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

        var hasActiveBookings = await _uow.Bookings.CountAsync(b =>
            b.ToolId == id &&
            (b.Status == BookingStatus.Pending ||
             b.Status == BookingStatus.Accepted ||
             b.Status == BookingStatus.DeliveryHandover ||
             b.Status == BookingStatus.Active ||
             b.Status == BookingStatus.ReturnHandover ||
             b.Status == BookingStatus.Disputed)) > 0;

        if (hasActiveBookings)
            throw new InvalidOperationException(
                "Cannot delete a tool that has active or pending bookings.");

        var imagePublicIds = await _uow.ToolImages.Query()
            .Where(i => i.ToolId == id && i.PublicId != null)
            .Select(i => i.PublicId!)
            .ToListAsync();

        _uow.Tools.Remove(tool);
        await _uow.SaveChangesAsync();

        foreach (var publicId in imagePublicIds)
        {
            try
            {
                await _cloudinaryService.DeleteImageAsync(publicId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Cloudinary image {PublicId} for removed tool {ToolId}", publicId, id);
            }
        }
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
        var hasPrimary = await _uow.ToolImages.Query()
            .AnyAsync(i => i.ToolId == toolId && i.IsPrimary);

        foreach (var img in images)
        {
            var uploadResult = await _cloudinaryService.UploadImageAsync(img);
            urls.Add(uploadResult.ImageUrl);

            await _uow.ToolImages.AddAsync(new ToolImage
            {
                ToolId = toolId,
                ImageUrl = uploadResult.ImageUrl,
                PublicId = uploadResult.PublicId,
                IsPrimary = !hasPrimary
            });

            hasPrimary = true;
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

        if (image.IsPrimary)
        {
            var nextPrimary = await _uow.ToolImages.Query()
                .Where(i => i.ToolId == image.ToolId && i.Id != image.Id)
                .OrderBy(i => i.Id)
                .FirstOrDefaultAsync();

            if (nextPrimary is not null)
            {
                // Query() is no-tracking, so re-attach before mutating or the
                // promotion is silently dropped on SaveChanges.
                nextPrimary.IsPrimary = true;
                _uow.ToolImages.Update(nextPrimary);
            }
        }

        await _uow.SaveChangesAsync();
    }

    public async Task SetPrimaryImageAsync(int imageId, int ownerId)
    {
        var image = await _uow.ToolImages.GetByIdWithToolAsync(imageId)
            ?? throw new KeyNotFoundException($"Image {imageId} not found");

        if (image.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Not your image");

        // Query() is no-tracking, so re-attach changed siblings or the update
        // is silently dropped on SaveChanges. The target image is already
        // tracked via GetByIdWithToolAsync, so it is excluded from the query.
        var siblings = await _uow.ToolImages.Query()
            .Where(i => i.ToolId == image.ToolId && i.Id != imageId)
            .ToListAsync();

        foreach (var sibling in siblings.Where(s => s.IsPrimary))
        {
            sibling.IsPrimary = false;
            _uow.ToolImages.Update(sibling);
        }

        image.IsPrimary = true;

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
