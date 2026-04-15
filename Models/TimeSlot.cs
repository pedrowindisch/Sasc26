namespace Sasc26.Models;

public class TimeSlot
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}
