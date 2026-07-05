using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.DTOs.Auth;

public record RegisterRequestDTO(
    string FullName,
    string Email,
    string PhoneNumber,
    string City,
    string Password,
    IFormFile? ProfileImage = null,
    IFormFile? NationalIdImage = null);
