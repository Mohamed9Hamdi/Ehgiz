namespace Ehgiz.Application.Settings;

public class FrontendSettings
{
    /// <summary>Base URL of the Angular app, used to build redirect/return URLs.</summary>
    public string BaseUrl { get; set; } = "http://localhost:4500";

    /// <summary>Origins allowed by CORS. Defaults to the local Angular dev server.</summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:4500"];
}
