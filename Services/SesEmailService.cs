using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;

namespace Sasc26.Services;

public class AwsSettings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string SenderEmail { get; set; } = string.Empty;
}

public class SesEmailService : IEmailService
{
    private readonly AwsSettings _settings;
    private readonly ILogger<SesEmailService> _logger;

    public SesEmailService(IOptions<AwsSettings> settings, ILogger<SesEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string email, string otpCode, string sessionName)
    {
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_settings.Region);
        var config = new AmazonSimpleEmailServiceConfig { RegionEndpoint = regionEndpoint };

        using var client = new AmazonSimpleEmailServiceClient(
            _settings.AccessKey, _settings.SecretKey, config);

        var sendRequest = new SendEmailRequest
        {
            Source = _settings.SenderEmail,
            Destination = new Destination { ToAddresses = [email] },
            Message = new Message
            {
                Subject = new Content($"Código de Verificação - {sessionName}"),
                Body = new Body
                {
                    Html = new Content($"""
                        <div style="font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;">
                            <h2 style="color:#1a1a2e;">SASC 26 - Semana Acadêmica</h2>
                            <p>Seu código de verificação para <strong>{sessionName}</strong> é:</p>
                            <div style="font-size:32px;font-weight:bold;letter-spacing:8px;
                                        background:#f0f0f0;padding:16px;text-align:center;
                                        border-radius:8px;margin:16px 0;">
                                {otpCode}
                            </div>
                            <p style="color:#666;font-size:14px;">
                                Este código é válido por 15 minutos. Não compartilhe com terceiros.
                            </p>
                        </div>
                        """)
                }
            }
        };

        await client.SendEmailAsync(sendRequest);
        _logger.LogInformation("OTP email sent to {Email} for session {Session}", email, sessionName);
    }
}

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(string email, string otpCode, string sessionName)
    {
        _logger.LogWarning("===== DEV EMAIL ===== To: {Email} | Session: {Session} | OTP: {Code}", email, sessionName, otpCode);
        return Task.CompletedTask;
    }
}
