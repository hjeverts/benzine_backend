namespace Vehictory.Api.Services;

public class AdminOptions
{
    public string BootstrapEmails { get; init; } = "";

    public HashSet<string> GetBootstrapEmails() =>
        BootstrapEmails
            .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(email => email.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
}
