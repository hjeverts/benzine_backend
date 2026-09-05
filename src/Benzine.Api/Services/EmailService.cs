using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Benzine.Api.Services;

public class EmailService(IOptions<EmailOptions> options)
{
    private readonly EmailOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured();

    public async Task SendPasswordResetAsync(string recipient, string token, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("E-mail is niet geconfigureerd.");

        var resetUrl = new UriBuilder(_options.FrontendUrl)
        {
            Path = "/reset-password",
            Query = $"email={Uri.EscapeDataString(recipient)}&token={Uri.EscapeDataString(token)}",
        }.Uri;

        using var message = new MailMessage(
            new MailAddress(_options.From, "Benzine"),
            new MailAddress(recipient))
        {
            Subject = "Stel je Benzine-wachtwoord opnieuw in",
            Body = $"""
                Je hebt een verzoek gedaan om je wachtwoord opnieuw in te stellen.

                Gebruik deze link om een nieuw wachtwoord te kiezen:
                {resetUrl}

                Deze link verloopt over één uur. Heb je dit niet aangevraagd? Dan kun je deze e-mail negeren.
                """,
        };

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
        };
        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        await client.SendMailAsync(message, cancellationToken);
    }
}
