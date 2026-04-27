using System.Text.Json;

namespace Sasc26.Models;

public class Event
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AllowedEmailDomain { get; set; } = "furb.br";
    public string InstagramUrl { get; set; } = string.Empty;
    public string TshirtPresaleUrl { get; set; } = string.Empty;
    public string AdminEmailsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Customization
    public string PrimaryColor { get; set; } = "#113D76";
    public string AccentColor { get; set; } = "#2EBDEF";
    public string BackgroundColor { get; set; } = "#f5f5f5";
    public string TextColor { get; set; } = "#1a1a1a";
    public byte[]? LogoImage { get; set; }
    public string LogoContentType { get; set; } = string.Empty;
    public string PostCheckinButtonsJson { get; set; } = "[]";

    public List<string> AdminEmails
    {
        get => string.IsNullOrWhiteSpace(AdminEmailsJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(AdminEmailsJson) ?? [];
        set => AdminEmailsJson = JsonSerializer.Serialize(value ?? []);
    }

    public List<PostCheckinButton> PostCheckinButtons
    {
        get => string.IsNullOrWhiteSpace(PostCheckinButtonsJson)
            ? []
            : JsonSerializer.Deserialize<List<PostCheckinButton>>(PostCheckinButtonsJson) ?? [];
        set => PostCheckinButtonsJson = JsonSerializer.Serialize(value ?? []);
    }
}

public class PostCheckinButton
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
