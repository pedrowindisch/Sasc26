namespace Sasc26.Services;

public class EventSettings
{
    public string AllowedEmailDomain { get; set; } = "furb.br";
    public int OtpExpirationMinutes { get; set; } = 15;
    public int MaxOtpAttemptsPerSession { get; set; } = 3;
    public string InstagramUrl { get; set; } = string.Empty;
    public string TshirtPresaleUrl { get; set; } = string.Empty;
    public List<string> AdminEmails { get; set; } = [];
    public List<TimeSlotConfig> TimeSlots { get; set; } = [];
}

public class TimeSlotConfig
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Shift { get; set; } = string.Empty;
}
