namespace Sasc26.Models;

public class IssuedCertificate
{
    public string ValidationCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public DateTime IssuedAt { get; set; }
}