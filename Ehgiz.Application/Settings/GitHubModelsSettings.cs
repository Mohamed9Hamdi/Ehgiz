namespace Ehgiz.Application.Settings;

public class GitHubModelsSettings
{
    public string ApiKey { get; set; } = null!;
    public string BaseUrl { get; set; } = "https://models.github.ai/inference";
    public string Model { get; set; } = "openai/gpt-4o";
    public int MaxImages { get; set; } = 5;
    public int MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
