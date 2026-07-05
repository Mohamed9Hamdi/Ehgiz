using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record RegisterRequestDTO(
    [property: Required, MaxLength(150)] string FullName,
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, Phone, MaxLength(30)] string PhoneNumber,
    [property: Required, MaxLength(100)] string City,
    [property: Required, MinLength(8), MaxLength(128)] string Password,
    IFormFile? ProfileImage = null,
    IFormFile? NationalIdImage = null);
