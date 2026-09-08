using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface IPreRegistrationConfigService
{
    Task<PreRegistrationConfigDto> GetConfigAsync();
    Task<PreRegistrationConfigDto> UpdateConfigAsync(PreRegistrationConfigDto dto);
    Task SubmitFormAsync(string email, List<FormFieldResponseDto> responses);
    Task<List<PreRegistrationFormSubmissionDto>> GetSubmissionsAsync();
}

public class PreRegistrationConfigService : IPreRegistrationConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    private readonly AppDbContext _db;
    private readonly IEventContext _eventContext;

    public PreRegistrationConfigService(AppDbContext db, IEventContext eventContext)
    {
        _db = db;
        _eventContext = eventContext;
    }

    private int EventId => _eventContext.CurrentEventId;

    public async Task<PreRegistrationConfigDto> GetConfigAsync()
    {
        var config = await _db.PreRegistrationConfigs.FirstOrDefaultAsync(c => c.EventId == EventId);
        if (config is null)
        {
            return new PreRegistrationConfigDto
            {
                Message = "Pré-inscrição realizada com sucesso!",
                IsFormEnabled = false,
                FormFields = []
            };
        }

        var fields = ParseFields(config.FormFields);
        return new PreRegistrationConfigDto
        {
            Message = config.Message,
            IsFormEnabled = config.IsFormEnabled,
            FormTitle = config.FormTitle,
            FormDescription = config.FormDescription,
            FormButtonText = config.FormButtonText,
            FormFields = fields
        };
    }

    public async Task<PreRegistrationConfigDto> UpdateConfigAsync(PreRegistrationConfigDto dto)
    {
        var config = await _db.PreRegistrationConfigs.FirstOrDefaultAsync(c => c.EventId == EventId);
        if (config is null)
        {
            config = new PreRegistrationConfig { EventId = EventId };
            _db.PreRegistrationConfigs.Add(config);
        }

        config.Message = dto.Message?.Trim() ?? string.Empty;
        config.IsFormEnabled = dto.IsFormEnabled;
        config.FormTitle = dto.FormTitle?.Trim() ?? string.Empty;
        config.FormDescription = dto.FormDescription?.Trim() ?? string.Empty;
        config.FormButtonText = string.IsNullOrWhiteSpace(dto.FormButtonText) ? "Concluir inscrição" : dto.FormButtonText.Trim();
        config.FormFields = JsonSerializer.Serialize(dto.FormFields ?? [], JsonOptions);

        await _db.SaveChangesAsync();
        return await GetConfigAsync();
    }

    public async Task SubmitFormAsync(string email, List<FormFieldResponseDto> responses)
    {
        var data = JsonSerializer.Serialize(responses, JsonOptions);

        _db.PreRegistrationFormSubmissions.Add(new PreRegistrationFormSubmission
        {
            AttendeeEmail = email,
            EventId = EventId,
            FormData = data,
            SubmittedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<PreRegistrationFormSubmissionDto>> GetSubmissionsAsync()
    {
        var submissions = await _db.PreRegistrationFormSubmissions
            .Where(s => s.EventId == EventId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(s => new PreRegistrationFormSubmissionDto
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
