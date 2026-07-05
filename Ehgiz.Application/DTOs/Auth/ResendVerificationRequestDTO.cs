using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record ResendVerificationRequestDTO(
    [Required, EmailAddress, MaxLength(256)] string Email
);
