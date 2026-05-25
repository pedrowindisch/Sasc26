namespace Sasc26.Models;

public class EventCourse
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int NumberOfSemesters { get; set; } = 8;
}
