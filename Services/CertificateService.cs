using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface ICertificateService
{
    Task<CertificateLookupResult> LookupProfileAsync(string email);
    Task<CertificateIssueResult> IssueOrUpdateCertificateAsync(CertificateRequestDto dto);
    Task<CertificateDisplayResult> GetCertificateAsync(string validationCode);
    Task<CertificateValidateResult> ValidateCertificateAsync(string email, string validationCode);
    Task<CertificateConfigDto> GetConfigAsync();
    Task<CertificateConfigDto> UpdateConfigAsync(CertificateConfigDto dto);
    Task UpdateBackgroundImageAsync(byte[] imageData, string contentType);
    Task RemoveBackgroundImageAsync();
    Task<List<IssuedCertificateDto>> GetAllIssuedCertificatesAsync();
}

public class CertificateLookupResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
}

public class CertificateIssueResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ValidationCode { get; set; } = string.Empty;
}

public class CertificateDisplayResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RenderedText { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public string ValidationCode { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string TitleColor { get; set; } = "#113D76";
    public string BodyColor { get; set; } = "#1a1a1a";
    public string BorderColor { get; set; } = "#113D76";
    public bool HasBackgroundImage { get; set; }
}

public class CertificateValidateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class CertificateService : ICertificateService
{
    private static readonly TimeZoneInfo BrasiliaTz = GetBrasiliaTimeZone();

    private readonly AppDbContext _db;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(AppDbContext db, ILogger<CertificateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CertificateLookupResult> LookupProfileAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.Email == email);
        if (volunteer is not null)
        {
            return new CertificateLookupResult
            {
                Success = true,
                Exists = true,
                Name = volunteer.Name,
                Course = volunteer.Course,
                Phase = volunteer.Semester.ToString()
            };
        }

        var attendee = await _db.Attendees.FirstOrDefaultAsync(a => a.Email == email);
        if (attendee is not null && !string.IsNullOrWhiteSpace(attendee.FullName))
        {
            return new CertificateLookupResult
            {
                Success = true,
                Exists = true,
                Name = attendee.FullName,
                Course = attendee.Course,
                Phase = attendee.Phase.ToString()
            };
        }

        return new CertificateLookupResult
        {
            Success = true,
            Exists = false
        };
    }

    public async Task<CertificateIssueResult> IssueOrUpdateCertificateAsync(CertificateRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var spectatorHours = await CalculateSpectatorHoursAsync(email);
        var volunteerHours = await CalculateVolunteerHoursAsync(email);
        var totalHours = spectatorHours + volunteerHours;

        if (totalHours <= 0)
            return new CertificateIssueResult { Success = false, Message = "Nenhuma presença registrada para emitir certificado." };

        var validationCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        var existing = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Email == email);
        if (existing is not null)
        {
            existing.Name = dto.Name.Trim();
            existing.Course = dto.Course;
            existing.Phase = dto.Phase;
            existing.TotalHours = totalHours;
            existing.IssuedAt = now;
            validationCode = existing.ValidationCode;
        }
        else
        {
            _db.IssuedCertificates.Add(new IssuedCertificate
            {
                ValidationCode = validationCode,
                Email = email,
                Name = dto.Name.Trim(),
                Course = dto.Course,
                Phase = dto.Phase,
                TotalHours = totalHours,
                IssuedAt = now
            });
        }

        await _db.SaveChangesAsync();
        return new CertificateIssueResult { Success = true, ValidationCode = validationCode };
    }

    public async Task<CertificateDisplayResult> GetCertificateAsync(string validationCode)
    {
        var cert = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.ValidationCode == validationCode);
        if (cert is null)
            return new CertificateDisplayResult { Success = false, Message = "Certificado não encontrado." };

        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        var template = config?.TemplateMessage ?? "Certificamos que {{nome}}, participou da SASC 26 com carga horária de {{horas}} horas.";

        var rendered = template
            .Replace("{{nome}}", cert.Name)
            .Replace("{{email}}", cert.Email)
            .Replace("{{curso}}", cert.Course)
            .Replace("{{fase}}", cert.Phase)
            .Replace("{{horas}}", cert.TotalHours.ToString("0"));

        return new CertificateDisplayResult
        {
            Success = true,
            RenderedText = rendered,
            Name = cert.Name,
            Course = cert.Course,
            Phase = cert.Phase,
            TotalHours = cert.TotalHours,
            ValidationCode = cert.ValidationCode,
            IssuedAt = cert.IssuedAt,
            TitleColor = config?.TitleColor ?? "#113D76",
            BodyColor = config?.BodyColor ?? "#1a1a1a",
            BorderColor = config?.BorderColor ?? "#113D76",
            HasBackgroundImage = config?.BackgroundImage != null && config.BackgroundImage.Length > 0
        };
    }

    public async Task<CertificateValidateResult> ValidateCertificateAsync(string email, string validationCode)
    {
        email = email.Trim().ToLowerInvariant();
        var code = validationCode.Trim().ToUpperInvariant();

        var cert = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Email == email && c.ValidationCode == code);
        if (cert is null)
            return new CertificateValidateResult { Success = false, Message = "Certificado não encontrado ou dados incorretos." };

        return new CertificateValidateResult
        {
            Success = true,
            Message = "Certificado Autêntico.",
            Name = cert.Name,
            Email = cert.Email,
            TotalHours = cert.TotalHours,
            IssuedAt = cert.IssuedAt
        };
    }

    public async Task<CertificateConfigDto> GetConfigAsync()
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        return new CertificateConfigDto
        {
            TemplateMessage = config?.TemplateMessage ?? string.Empty,
            TitleColor = config?.TitleColor ?? "#113D76",
            BodyColor = config?.BodyColor ?? "#1a1a1a",
            BorderColor = config?.BorderColor ?? "#113D76",
            HasBackgroundImage = config?.BackgroundImage != null && config.BackgroundImage.Length > 0
        };
    }

    public async Task<CertificateConfigDto> UpdateConfigAsync(CertificateConfigDto dto)
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new CertificateConfig { TemplateMessage = dto.TemplateMessage };
            _db.CertificateConfigs.Add(config);
        }
        else
        {
            config.TemplateMessage = dto.TemplateMessage;
        }

        if (!string.IsNullOrWhiteSpace(dto.TitleColor)) config.TitleColor = dto.TitleColor;
        if (!string.IsNullOrWhiteSpace(dto.BodyColor)) config.BodyColor = dto.BodyColor;
        if (!string.IsNullOrWhiteSpace(dto.BorderColor)) config.BorderColor = dto.BorderColor;

        await _db.SaveChangesAsync();
        return await GetConfigAsync();
    }

    public async Task UpdateBackgroundImageAsync(byte[] imageData, string contentType)
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new CertificateConfig();
            _db.CertificateConfigs.Add(config);
        }
        config.BackgroundImage = imageData;
        config.BackgroundImageContentType = contentType;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveBackgroundImageAsync()
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        if (config is not null)
        {
            config.BackgroundImage = null;
            config.BackgroundImageContentType = string.Empty;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<IssuedCertificateDto>> GetAllIssuedCertificatesAsync()
    {
        return await _db.IssuedCertificates
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new IssuedCertificateDto
            {
                ValidationCode = c.ValidationCode,
                Email = c.Email,
                Name = c.Name,
                Course = c.Course,
                Phase = c.Phase,
                TotalHours = c.TotalHours,
                IssuedAt = c.IssuedAt
            })
            .ToListAsync();
    }

    private async Task<decimal> CalculateSpectatorHoursAsync(string email)
    {
        return await _db.CheckIns
            .Where(c => c.AttendeeEmail == email && c.Status == CheckInStatus.Verified && c.Lecture != null)
            .Include(c => c.Lecture)
            .ThenInclude(l => l!.TimeSlot)
            .SumAsync(c => (decimal?)c.Lecture!.TimeSlot.CreditHours) ?? 0;
    }

    private async Task<decimal> CalculateVolunteerHoursAsync(string email)
    {
        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.Email == email);
        if (volunteer is null) return 0;

        return await _db.VolunteerCheckIns
            .Where(c => c.VolunteerId == volunteer.Id)
            .Include(c => c.TimeSlot)
            .SumAsync(c => (decimal?)c.TimeSlot.CreditHours * 2) ?? 0;
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}