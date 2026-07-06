using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.DTOs.Profile;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class AuthProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterRequestDTO, ApplicationUser>()
            .Map(dest => dest.UserName, src => src.Email)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.IsActive, _ => true);

        config.NewConfig<AuthTokensDTO, LoginResponseDTO>()
            .Map(dest => dest.ExpiresAt, src => src.AccessTokenExpiresAt)
            .Map(dest => dest.Roles, src => src.Roles);

        config.NewConfig<ApplicationUser, UserProfileDTO>()
            .Map(dest => dest.Roles, _ => Array.Empty<string>());

        // Role and the listing/booking counts are resolved separately and
        // injected by the caller.
        config.NewConfig<ApplicationUser, AdminUserDetailsDto>()
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Ignore(dest => dest.Role)
            .Ignore(dest => dest.TotalListings)
            .Ignore(dest => dest.TotalBookings);
    }
}
