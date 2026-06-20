namespace Ehgiz.Application.Settings;

public class SendGridSettings
{
    public string ApiKey { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public int VerificationCodeMins { get; set; } = 15;
}
