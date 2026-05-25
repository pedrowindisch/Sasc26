namespace Sasc26.Models;

public class VolunteerCheckIn
{
    public Guid Id { get; set; }
    public Guid VolunteerId { get; set; }
    public Volunteer Volunteer { get; set; } = null!;
    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}
