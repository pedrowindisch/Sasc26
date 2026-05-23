namespace Sasc26.Models;

public class PreRegistrationConfig
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Message { get; set; } = "Pré-inscrição realizada com sucesso!";
    public bool IsFormEnabled { get; set; }
    public string FormTitle { get; set; } = string.Empty;
    public string FormDescription { get; set; } = string.Empty;
    public string FormButtonText { get; set; } = "Concluir inscrição";
    public string FormFields { get; set; } = "[]";
}
