namespace Sasc26.Models;

public class CheckIn
{
    public Guid Id { get; set; }
    public string AttendeeEmail { get; set; } = string.Empty;
    public Attendee Attendee { get; set; } = null!;
    public int? LectureId { get; set; }
    public Lecture? Lecture { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public CheckInStatus Status { get; set; } = CheckInStatus.Pending;
    public bool SesFallback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}
