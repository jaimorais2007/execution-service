using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly Func<ISmtpClient>? _smtpClientFactory;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        : this(configuration, logger, null)
    {
    }

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, Func<ISmtpClient>? smtpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpClientFactory = smtpClientFactory;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var from = _configuration["EmailSettings:Email"] ?? "noreplyoficinamecanica@gmail.com";
        var host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
        var portStr = _configuration["EmailSettings:Port"] ?? "587";
        var password = _configuration["EmailSettings:Password"];
        var port = int.TryParse(portStr, out var p) ? p : 587;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Oficina Mecânica", from));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = _smtpClientFactory != null ? _smtpClientFactory() : new SmtpClient();

            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);

            if (!string.IsNullOrWhiteSpace(password))
            {
                await client.AuthenticateAsync(from, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}. Subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Recipient}. Subject: {Subject}. Host: {Host}, Port: {Port}. ErrorType: {ErrorType}. Message: {ErrorMessage}",
                to,
                subject,
                host,
                port,
                ex.GetType().Name,
                ex.Message);
            throw;
        }
    }
}
