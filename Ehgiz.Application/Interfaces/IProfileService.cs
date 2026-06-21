using Ehgiz.Application.DTOs.Profile;

namespace Ehgiz.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDTO?> GetProfileAsync(int userId);
}
