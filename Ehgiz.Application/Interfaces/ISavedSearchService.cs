using Ehgiz.Application.DTOs.SavedSearches;

namespace Ehgiz.Application.Interfaces;

public interface ISavedSearchService
{
    Task<SavedSearchDto> CreateAsync(int userId, CreateSavedSearchDto dto);
    Task<List<SavedSearchDto>> GetAllForUserAsync(int userId);
    Task DeleteAsync(int id, int userId);

    /// <summary>Notifies users whose saved searches match a newly listed tool.</summary>
    Task NotifyMatchesAsync(int toolId);
}
