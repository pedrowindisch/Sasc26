namespace Sasc26.Models;

public class FormSubmission
{
    public int Id { get; set; }
    public string AttendeeEmail { get; set; } = string.Empty;
    public string FormData { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}
