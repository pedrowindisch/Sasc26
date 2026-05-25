namespace Sasc26.Models;

public class CertificateConfig
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string TemplateMessage { get; set; } = "Certificamos que {{nome}}, aluno(a) da {{fase}} fase do curso de {{curso}}, participou da {{evento}}, com carga horária total de {{horas}} horas.";
    public byte[]? BackgroundImage { get; set; }
    public string BackgroundImageContentType { get; set; } = string.Empty;
    public string TitleColor { get; set; } = "#113D76";
    public string BodyColor { get; set; } = "#1a1a1a";
    public string BorderColor { get; set; } = "#113D76";
}
