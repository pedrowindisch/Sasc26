namespace Sasc26.Models;

public class PreRegistration
{
    public int Id { get; set; }
    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;
    public string AttendeeEmail { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
}
