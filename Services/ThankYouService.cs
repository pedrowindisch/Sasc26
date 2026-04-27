using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface IThankYouService
{
    Task<ThankYouConfigDto> GetConfigAsync();
    Task<ThankYouConfigDto> UpdateConfigAsync(ThankYouConfigDto dto);
    Task SubmitFormAsync(SubmitFormDto dto);
    Task<List<FormSubmissionDto>> GetSubmissionsAsync();
}

public class ThankYouService : IThankYouService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AppDbContext _db;
    private readonly IEventContext _eventContext;

    public ThankYouService(AppDbContext db, IEventContext eventContext)
    {
        _db = db;
        _eventContext = eventContext;
    }

    private int EventId => _eventContext.CurrentEventId;
    private string EventName => _eventContext.CurrentEvent.Name;

    public async Task<ThankYouConfigDto> GetConfigAsync()
    {
        var config = await _db.ThankYouConfigs.FirstOrDefaultAsync(c => c.EventId == EventId);
        if (config is null)
        {
            return new ThankYouConfigDto
            {
                Message = $"Obrigado por participar da {EventName}!",
                IsFormEnabled = false,
                FormFields = []
            };
        }

        var fields = ParseFields(config.FormFields);
        return new ThankYouConfigDto
        {
            Message = config.Message,
            IsFormEnabled = config.IsFormEnabled,
            FormTitle = config.FormTitle,
            FormDescription = config.FormDescription,
            FormButtonText = config.FormButtonText,
            FormFields = fields
        };
    }

    public async Task<ThankYouConfigDto> UpdateConfigAsync(ThankYouConfigDto dto)
    {
        var config = await _db.ThankYouConfigs.FirstOrDefaultAsync(c => c.EventId == EventId);
        if (config is null)
        {
            config = new ThankYouConfig { EventId = EventId };
            _db.ThankYouConfigs.Add(config);
        }

        config.Message = dto.Message?.Trim() ?? string.Empty;
        config.IsFormEnabled = dto.IsFormEnabled;
        config.FormTitle = dto.FormTitle?.Trim() ?? string.Empty;
        config.FormDescription = dto.FormDescription?.Trim() ?? string.Empty;
        config.FormButtonText = string.IsNullOrWhiteSpace(dto.FormButtonText) ? "Enviar" : dto.FormButtonText.Trim();
        config.FormFields = JsonSerializer.Serialize(dto.FormFields ?? [], JsonOptions);

        await _db.SaveChangesAsync();
        return await GetConfigAsync();
    }

    public async Task SubmitFormAsync(SubmitFormDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var data = JsonSerializer.Serialize(dto.Responses, JsonOptions);

        _db.FormSubmissions.Add(new FormSubmission
        {
            AttendeeEmail = email,
            EventId = EventId,
            FormData = data,
            SubmittedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<FormSubmissionDto>> GetSubmissionsAsync()
    {
        var submissions = await _db.FormSubmissions
            .Where(s => s.EventId == EventId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(s => new FormSubmissionDto
        {
            Id = s.Id,
            AttendeeEmail = s.AttendeeEmail,
            Responses = ParseFieldsResponse(s.FormData),
            SubmittedAt = s.SubmittedAt
        }).ToList();
    }

    private static List<FormFieldDto> ParseFields(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<FormFieldDto>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }

    private static List<FormFieldResponseDto> ParseFieldsResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<FormFieldResponseDto>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }
}
