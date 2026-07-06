using Ehgiz.Application.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDTO?> GetProfileAsync(int userId);
    Task<UserProfileDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto);
    Task<UserProfileDTO?> UpdateProfileImageAsync(int userId, IFormFile image);
    Task<UserProfileDTO?> RemoveProfileImageAsync(int userId);
    Task<UserProfileDTO?> UpdateNationalIdImageAsync(int userId, IFormFile image);
}
