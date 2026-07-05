using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record ForgotPasswordRequestDTO(
    [Required, EmailAddress, MaxLength(256)] string Email
);
