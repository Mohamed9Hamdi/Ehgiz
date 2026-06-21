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
            .Map(dest => dest.ExpiresAt, src => src.AccessTokenExpiresAt);

        config.NewConfig<ApplicationUser, UserProfileDTO>()
            .Map(dest => dest.Roles, _ => Array.Empty<string>());
    }
}
