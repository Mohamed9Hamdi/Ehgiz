using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record RegisterRequestDTO(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, Phone, MaxLength(30)] string PhoneNumber,
    [Required, MaxLength(100)] string City,
    [Required, MinLength(8), MaxLength(128)] string Password,
    IFormFile? ProfileImage = null
);
