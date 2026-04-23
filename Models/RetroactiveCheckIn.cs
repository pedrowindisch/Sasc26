namespace Sasc26.Models;

public class RetroactiveCheckIn
{
    public Guid Id { get; set; }
    public string AttendeeEmail { get; set; } = string.Empty;
    public Attendee Attendee { get; set; } = null!;
    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;
    public RetroactiveCheckInStatus Status { get; set; } = RetroactiveCheckInStatus.Pending;
    public string Justification { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum RetroactiveCheckInStatus
{
    Pending,
    Approved,
    Rejected
}