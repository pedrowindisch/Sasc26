namespace Sasc26.Models;

public class MagicCheckInSession
{
    public Guid Id { get; set; }
    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}
