using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record ResendVerificationRequestDTO(
    [property: Required, EmailAddress, MaxLength(256)] string Email);
