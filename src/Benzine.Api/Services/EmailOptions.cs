namespace Benzine.Api.Services;

public class EmailOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string From { get; init; } = "";
    public string FrontendUrl { get; init; } = "";

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(From)
        && Uri.TryCreate(FrontendUrl, UriKind.Absolute, out _);
}
