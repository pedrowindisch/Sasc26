namespace Sasc26.Models;

public class Banner
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CtaText { get; set; } = string.Empty;
    public string CtaUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
