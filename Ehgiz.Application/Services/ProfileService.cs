using Ehgiz.Application.DTOs.Profile;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Ehgiz.Application.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IMapper _mapper;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        ICloudinaryService cloudinaryService,
        IMapper mapper)
    {
        _userManager = userManager;
        _cloudinaryService = cloudinaryService;
        _mapper = mapper;
    }

    public async Task<UserProfileDTO?> GetProfileAsync(int userId)
    {
        var user = await GetActiveUserAsync(userId);
        if (user is null)
            return null;

        return await MapProfileAsync(user);
    }

    public async Task<UserProfileDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
    {
        var user = await GetActiveUserAsync(userId);
        if (user is null)
            return null;

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber.Trim();
        if (dto.Address is not null) user.Address = dto.Address.Trim();
        if (dto.City is not null) user.City = dto.City.Trim();

        await _userManager.UpdateAsync(user);
        return await MapProfileAsync(user);
    }

    public async Task<UserProfileDTO?> UpdateProfileImageAsync(int userId, IFormFile image)
    {
        var user = await GetActiveUserAsync(userId);
        if (user is null)
            return null;

        var upload = await _cloudinaryService.UploadImageAsync(image);

        if (!string.IsNullOrWhiteSpace(user.ProfileImagePublicId))
            await _cloudinaryService.DeleteImageAsync(user.ProfileImagePublicId);

        user.ProfileImageUrl = upload.ImageUrl;
        user.ProfileImagePublicId = upload.PublicId;
        await _userManager.UpdateAsync(user);

        return await MapProfileAsync(user);
    }

    public async Task<UserProfileDTO?> UpdateNationalIdImageAsync(int userId, IFormFile image)
    {
        var user = await GetActiveUserAsync(userId);
        if (user is null)
            return null;

        var upload = await _cloudinaryService.UploadImageAsync(image);

        if (!string.IsNullOrWhiteSpace(user.NationalIdImagePublicId))
            await _cloudinaryService.DeleteImageAsync(user.NationalIdImagePublicId);

        user.NationalIdImageUrl = upload.ImageUrl;
        user.NationalIdImagePublicId = upload.PublicId;
        await _userManager.UpdateAsync(user);

        return await MapProfileAsync(user);
    }

    public async Task<UserProfileDTO?> RemoveProfileImageAsync(int userId)
    {
        var user = await GetActiveUserAsync(userId);
        if (user is null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.ProfileImagePublicId))
            await _cloudinaryService.DeleteImageAsync(user.ProfileImagePublicId);

        user.ProfileImageUrl = null;
        user.ProfileImagePublicId = null;
        await _userManager.UpdateAsync(user);

        return await MapProfileAsync(user);
    }

    private async Task<ApplicationUser?> GetActiveUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null || !user.IsActive ? null : user;
    }

    private async Task<UserProfileDTO> MapProfileAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var profile = _mapper.Map<UserProfileDTO>(user);
        return profile with { Roles = roles.ToList() };
    }
}
