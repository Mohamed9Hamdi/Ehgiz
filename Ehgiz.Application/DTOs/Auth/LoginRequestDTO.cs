namespace Ehgiz.Application.DTOs.Auth;

public record LoginRequestDTO(
    string Email,
    string Password);
