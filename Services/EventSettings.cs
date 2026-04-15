using Sasc26.Models;

namespace Sasc26.Services;

public class EventSettings
{
    public string AllowedEmailDomain { get; set; } = "furb.br";
    public int OtpExpirationMinutes { get; set; } = 15;
    public int MaxOtpAttemptsPerSession { get; set; } = 3;
    public List<SessionConfig> Sessions { get; set; } = [];
}

public class SessionConfig
{
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
