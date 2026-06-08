namespace Ehgiz.Application.DTOs.Auth;

public record RegisterRequestDTO(
    string FullName,
    string Email,
    string PhoneNumber,
    string City,
    string Password);
