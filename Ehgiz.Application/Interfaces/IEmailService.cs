namespace Ehgiz.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string toEmail, string code);
    Task SendPasswordResetCodeAsync(string toEmail, string code);
}
