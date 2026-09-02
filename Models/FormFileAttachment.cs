namespace Sasc26.Models;

public class FormFileAttachment
{
    public Guid Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int? PreRegistrationSubmissionId { get; set; }
    public PreRegistrationFormSubmission? PreRegistrationSubmission { get; set; }
    public int? FormSubmissionId { get; set; }
    public FormSubmission? FormSubmission { get; set; }
    public string FieldLabel { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public byte[] CompressedData { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
