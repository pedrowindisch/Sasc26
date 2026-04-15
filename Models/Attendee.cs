namespace Sasc26.Models;

public class Attendee
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
