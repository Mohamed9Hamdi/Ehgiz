using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record VerifyEmailRequestDTO(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(10)] string Code);
