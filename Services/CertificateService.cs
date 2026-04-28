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
    Task<byte[]> ExportAllCertificatesZipAsync();
    Task ExportAllCertificatesToFileAsync(string filePath);
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

    public async Task<byte[]> ExportAllCertificatesZipAsync()
    {
        using var ms = new MemoryStream();
        await WriteExportZipToStreamAsync(ms);
        return ms.ToArray();
    }

    public async Task ExportAllCertificatesToFileAsync(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await WriteExportZipToStreamAsync(fs);
    }

    private async Task WriteExportZipToStreamAsync(Stream destination)
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        var template = config?.TemplateMessage ?? "Certificamos que {{nome}}, participou da SASC 26 com carga horária de {{horas}} horas.";
        var titleColor = config?.TitleColor ?? "#113D76";
        var bodyColor = config?.BodyColor ?? "#1a1a1a";
        var borderColor = config?.BorderColor ?? "#113D76";
        var hasBg = config?.BackgroundImage != null && config.BackgroundImage.Length > 0;
        string? backgroundImageDataUri = null;
        if (hasBg && config!.BackgroundImage is not null)
        {
            var contentType = string.IsNullOrEmpty(config.BackgroundImageContentType) ? "image/png" : config.BackgroundImageContentType;
            backgroundImageDataUri = $"data:{contentType};base64,{Convert.ToBase64String(config.BackgroundImage)}";
        }

        // 1. Already-issued certificates
        var existingCerts = await _db.IssuedCertificates.ToListAsync();
        var existingEmails = existingCerts.Select(c => c.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 2. Spectators with verified check-ins who don't have a certificate yet
        var spectatorCandidates = await _db.Attendees
            .Where(a => a.CheckIns.Any(c => c.Status == CheckInStatus.Verified))
            .ToListAsync();

        // 3. Volunteers with check-ins who don't have a certificate yet
        var volunteerCandidates = await _db.Volunteers
            .Where(v => v.CheckIns.Any())
            .ToListAsync();

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        var newCerts = new List<IssuedCertificate>();

        foreach (var attendee in spectatorCandidates)
        {
            if (existingEmails.Contains(attendee.Email)) continue;

            var spectatorHours = await CalculateSpectatorHoursAsync(attendee.Email);
            var volunteerHours = await CalculateVolunteerHoursAsync(attendee.Email);
            var totalHours = spectatorHours + volunteerHours;
            if (totalHours <= 0) continue;

            newCerts.Add(new IssuedCertificate
            {
                ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                Email = attendee.Email,
                Name = attendee.FullName,
                Course = attendee.Course,
                Phase = attendee.Phase.ToString(),
                TotalHours = totalHours,
                IssuedAt = now
            });
        }

        foreach (var volunteer in volunteerCandidates)
        {
            if (existingEmails.Contains(volunteer.Email)) continue;

            var spectatorHours = await CalculateSpectatorHoursAsync(volunteer.Email);
            var volunteerHours = await CalculateVolunteerHoursAsync(volunteer.Email);
            var totalHours = spectatorHours + volunteerHours;
            if (totalHours <= 0) continue;

            newCerts.Add(new IssuedCertificate
            {
                ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                Email = volunteer.Email,
                Name = volunteer.Name,
                Course = volunteer.Course,
                Phase = volunteer.Semester.ToString(),
                TotalHours = totalHours,
                IssuedAt = now
            });
        }

        // 4. Deduplicate by email (same person may appear in both spectator and volunteer lists)
        newCerts = newCerts.GroupBy(c => c.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        // 5. Persist new certificates to the database so validation codes work on the website.
        //    Use upsert logic to avoid unique constraint violations on Email.
        foreach (var newCert in newCerts)
        {
            var existing = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Email == newCert.Email);
            if (existing is not null)
            {
                existing.Name = newCert.Name;
                existing.Course = newCert.Course;
                existing.Phase = newCert.Phase;
                existing.TotalHours = newCert.TotalHours;
                existing.IssuedAt = newCert.IssuedAt;
                // Keep the existing validation code so it remains valid on the website
            }
            else
            {
                _db.IssuedCertificates.Add(newCert);
            }
        }
        await _db.SaveChangesAsync();

        // 5. Combine all certificates
        var allCerts = existingCerts.Concat(newCerts)
            .OrderByDescending(c => c.IssuedAt)
            .ToList();

        using (var archive = new System.IO.Compression.ZipArchive(destination, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            // CSV summary entry
            var csvEntry = archive.CreateEntry("certificados_resumo.csv");
            using (var csvWriter = new StreamWriter(csvEntry.Open()))
            {
                await csvWriter.WriteLineAsync("Nome,Email,Curso,Fase,TotalHoras,CodigoValidacao,DataEmissao");
                foreach (var cert in allCerts)
                {
                    var name = EscapeCsv(cert.Name);
                    var email = EscapeCsv(cert.Email);
                    var course = EscapeCsv(cert.Course);
                    var phase = EscapeCsv(cert.Phase);
                    await csvWriter.WriteLineAsync($"{name},{email},{course},{phase},{cert.TotalHours},{cert.ValidationCode},{cert.IssuedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }

            // Generate certificate HTML files in parallel
            var lockObj = new object();

            await Parallel.ForEachAsync(allCerts, async (cert, ct) =>
            {
                var rendered = template
                    .Replace("{{nome}}", cert.Name)
                    .Replace("{{email}}", cert.Email)
                    .Replace("{{curso}}", cert.Course)
                    .Replace("{{fase}}", cert.Phase)
                    .Replace("{{horas}}", cert.TotalHours.ToString("0"));

                var html = BuildCertificateHtml(cert, rendered, titleColor, bodyColor, borderColor, backgroundImageDataUri);

                var safeName = SanitizeFileName(cert.Name);
                var fileName = $"{safeName}_{cert.ValidationCode}.html";

                lock (lockObj)
                {
                    var entry = archive.CreateEntry(fileName);
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(html);
                }
            });
        }
    }

    private static string BuildCertificateHtml(IssuedCertificate cert, string renderedText, string titleColor, string bodyColor, string borderColor, string? backgroundImageDataUri)
    {
        var bgStyle = !string.IsNullOrEmpty(backgroundImageDataUri) ? $" style=\"background-image: url('{backgroundImageDataUri}');\"" : "";
        var issuedAt = cert.IssuedAt.ToString("dd/MM/yyyy HH:mm");

        return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Certificado - SASC 26</title>
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
    <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Playfair+Display:wght@400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: 'Inter', sans-serif; background: #fff; color: {bodyColor}; }}
        @@media print {{
            body {{ background: #fff; }}
            .cert-page {{ box-shadow: none !important; margin: 0 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; color-adjust: exact !important; }}
        }}
        .cert-page {{
            max-width: 800px;
            margin: 40px auto;
            padding: 60px 50px;
            border: 3px solid {borderColor};
            border-radius: 4px;
            position: relative;
            min-height: 500px;
            background-size: cover;
            background-position: center;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
            color-adjust: exact;
        }}
        .cert-page::before, .cert-page::after {{
            content: '';
            position: absolute;
            width: 40px; height: 40px;
            border: 2px solid {borderColor};
        }}
        .cert-page::before {{ top: 10px; left: 10px; border-right: none; border-bottom: none; }}
        .cert-page::after {{ bottom: 10px; right: 10px; border-left: none; border-top: none; }}
        .cert-header {{ text-align: center; padding-bottom: 30px; border-bottom: 1px solid #ddd; margin-bottom: 30px; }}
        .cert-header h1 {{ font-family: 'Playfair Display', serif; font-size: 2rem; color: {titleColor}; margin-bottom: 4px; }}
        .cert-header p {{ font-size: 0.8rem; color: #888; text-transform: uppercase; letter-spacing: 0.1em; }}
        .cert-body {{ text-align: center; padding: 20px 0; line-height: 1.8; font-size: 1rem; }}
        .cert-body .cert-text {{ font-size: 1rem; line-height: 1.9; }}
        .cert-body .cert-name {{ font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 700; color: {titleColor}; margin: 10px 0; }}
        .cert-hours {{ font-size: 1.3rem; font-weight: 700; color: {titleColor}; margin: 10px 0; }}
        .cert-footer {{ margin-top: 40px; text-align: center; font-size: 0.7rem; color: #aaa; }}
        .cert-footer .val-code {{ font-family: monospace; font-size: 0.75rem; color: #888; margin-top: 6px; }}
    </style>
</head>
<body>
    <div class=""cert-page""{bgStyle}>
        <div class=""cert-header"">
            <h1>SASC 26</h1>
            <p>Semana Acadêmica de Sistemas e Computação</p>
        </div>
        <div class=""cert-body"">
            <div class=""cert-text"">{renderedText.Replace("\n", "<br>")}</div>
            <div class=""cert-hours"">{cert.TotalHours} hora(s)</div>
        </div>
        <div class=""cert-footer"">
            <div>Código de validação: <span class=""val-code"">{cert.ValidationCode}</span></div>
            <div style=""margin-top:2px;"">Emitido em: {issuedAt}</div>
        </div>
    </div>
</body>
</html>";
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "certificado";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "certificado" : sanitized.Replace(" ", "_");
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