using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.SavedSearches;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class SavedSearchService : ISavedSearchService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public SavedSearchService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<SavedSearchDto> CreateAsync(int userId, CreateSavedSearchDto dto)
    {
        var hasCriteria =
            !string.IsNullOrWhiteSpace(dto.SearchTerm) ||
            dto.CategoryId.HasValue ||
            !string.IsNullOrWhiteSpace(dto.Location) ||
            dto.MinPrice.HasValue ||
            dto.MaxPrice.HasValue ||
            dto.Condition.HasValue;

        if (!hasCriteria)
            throw new ValidationException("At least one search criterion is required.");

        if (dto.MinPrice.HasValue && dto.MaxPrice.HasValue && dto.MinPrice > dto.MaxPrice)
            throw new ValidationException("MinPrice cannot be greater than MaxPrice.");

        if (dto.CategoryId.HasValue)
        {
            var categoryCount = await _uow.Categories.CountAsync(c => c.Id == dto.CategoryId);
            if (categoryCount == 0)
                throw new KeyNotFoundException($"Category {dto.CategoryId} not found");
        }

        var savedSearch = new SavedSearch
        {
            UserId = userId,
            SearchTerm = string.IsNullOrWhiteSpace(dto.SearchTerm) ? null : dto.SearchTerm.Trim(),
            CategoryId = dto.CategoryId,
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            MinPrice = dto.MinPrice,
            MaxPrice = dto.MaxPrice,
            Condition = dto.Condition,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.SavedSearches.AddAsync(savedSearch);
        await _uow.SaveChangesAsync();

        return await GetByIdAsync(savedSearch.Id);
    }

    public async Task<List<SavedSearchDto>> GetAllForUserAsync(int userId)
    {
        return await _uow.SavedSearches.Query()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ProjectToType<SavedSearchDto>()
            .ToListAsync();
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var savedSearch = await _uow.SavedSearches.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Saved search {id} not found");

        if (savedSearch.UserId != userId)
            throw new UnauthorizedAccessException("Not your saved search");

        _uow.SavedSearches.Remove(savedSearch);
        await _uow.SaveChangesAsync();
    }

    public async Task NotifyMatchesAsync(int toolId)
    {
        var tool = await _uow.Tools.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == toolId);

        if (tool is null)
            return;

        // Structured criteria are filtered in SQL; the text criteria
        // (search term / location substrings) are checked in memory below.
        var candidates = await _uow.SavedSearches.Query()
            .AsNoTracking()
            .Where(s => s.UserId != tool.OwnerId)
            .Where(s => s.CategoryId == null || s.CategoryId == tool.CategoryId)
            .Where(s => s.MinPrice == null || tool.PricePerDay >= s.MinPrice)
            .Where(s => s.MaxPrice == null || tool.PricePerDay <= s.MaxPrice)
            .Where(s => s.Condition == null || s.Condition == tool.Condition)
            .ToListAsync();

        var matchedUserIds = candidates
            .Where(s => MatchesText(tool, s))
            .Select(s => s.UserId)
            .Distinct()
            .ToList();

        foreach (var matchedUserId in matchedUserIds)
        {
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = matchedUserId,
                Title = "New match for your saved search",
                Message = $"\"{tool.Name}\" was just listed and matches one of your saved searches.",
                Type = NotificationType.SavedSearchMatch,
                Url = $"/tools/{tool.Id}"
            });
        }
    }

    private static bool MatchesText(Tool tool, SavedSearch search)
    {
        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            var termMatches =
                tool.Name.Contains(search.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                (tool.Description?.Contains(search.SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false);

            if (!termMatches)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(search.Location) &&
            !(tool.Location?.Contains(search.Location, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return false;
        }

        return true;
    }

    private async Task<SavedSearchDto> GetByIdAsync(int id)
    {
        return await _uow.SavedSearches.Query()
            .Where(s => s.Id == id)
            .ProjectToType<SavedSearchDto>()
            .FirstAsync();
    }
}
