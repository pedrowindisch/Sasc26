namespace Sasc26.Models;

public class Session
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
