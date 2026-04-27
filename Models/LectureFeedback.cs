namespace Sasc26.Models;

public class LectureFeedback
{
    public int Id { get; set; }
    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;
    public string AttendeeEmail { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool Skipped { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}
