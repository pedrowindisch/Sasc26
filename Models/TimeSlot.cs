namespace Sasc26.Models;

public class TimeSlot
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Shift { get; set; } = string.Empty;
    public int CreditHours { get; set; } = 2;
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}
