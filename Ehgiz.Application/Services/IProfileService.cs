using Ehgiz.Application.DTOs.Profile;

namespace Ehgiz.Application.Services;

public interface IProfileService
{
    Task<UserProfileDTO?> GetProfileAsync(int userId);
}
