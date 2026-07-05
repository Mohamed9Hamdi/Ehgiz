using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record VerifyEmailRequestDTO(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(10)] string Code
);
