using Ehgiz.Application.DTOs.Profile;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;

namespace Ehgiz.Application.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public ProfileService(UserManager<ApplicationUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserProfileDTO?> GetProfileAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var profile = _mapper.Map<UserProfileDTO>(user);
        return profile with { Roles = roles.ToList() };
    }
}
