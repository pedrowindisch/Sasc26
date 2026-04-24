namespace Sasc26.Models;

public class ThankYouConfig
{
    public int Id { get; set; } = 1;
    public string Message { get; set; } = "Obrigado por participar da SASC 26!";
    public bool IsFormEnabled { get; set; }
    public string FormTitle { get; set; } = string.Empty;
    public string FormDescription { get; set; } = string.Empty;
    public string FormButtonText { get; set; } = "Enviar";
    public string FormFields { get; set; } = "[]";
}