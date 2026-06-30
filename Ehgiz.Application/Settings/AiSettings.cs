namespace Ehgiz.Application.Settings;

public class AiSettings
{
    public string Provider { get; set; } = "GitHubModels";
    public string Endpoint { get; set; } = "https://models.github.ai/inference";
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public string ApiKey { get; set; } = string.Empty;
}
