namespace Sasc26.Models;

public class Volunteer
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Semester { get; set; }
    public bool IsVerified { get; set; }
    public DateTime RegisteredAt { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public ICollection<VolunteerCheckIn> CheckIns { get; set; } = new List<VolunteerCheckIn>();
}
