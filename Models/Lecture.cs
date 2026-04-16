namespace Sasc26.Models;

public class Lecture
{
    public int Id { get; set; }
    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Keyword1 { get; set; } = string.Empty;
    public string Keyword2 { get; set; } = string.Empty;
    public string Keyword3 { get; set; } = string.Empty;
    public bool IsPreRegistrationEnabled { get; set; }
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    public ICollection<PreRegistration> PreRegistrations { get; set; } = new List<PreRegistration>();
}
