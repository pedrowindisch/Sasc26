namespace Sasc26.Models;

public class CheckIn
{
    public Guid Id { get; set; }
    public string AttendeeEmail { get; set; } = string.Empty;
    public Attendee Attendee { get; set; } = null!;
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public string OtpCode { get; set; } = string.Empty;
    public CheckInStatus Status { get; set; } = CheckInStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
