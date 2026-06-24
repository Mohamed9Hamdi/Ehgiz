using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ehgiz.Application.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridSettings _settings;

    public SendGridEmailService(IOptions<SendGridSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(_settings.SenderEmail, "Ehgiz");
        var to = new EmailAddress(toEmail);
        var subject = "Verify your Ehgiz account";
        var plainText = $"Your verification code is: {code}\n\nThis code expires in {_settings.VerificationCodeMins} minutes.";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, null);

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"SendGrid failed ({response.StatusCode}): {body}");
        }
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string code)
    {
        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(_settings.SenderEmail, "Ehgiz");
        var to = new EmailAddress(toEmail);
        var subject = "Reset your Ehgiz password";
        var plainText = $"Your password reset code is: {code}\n\nThis code expires in {_settings.VerificationCodeMins} minutes.";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, null);

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"SendGrid failed ({response.StatusCode}): {body}");
        }
    }
}
