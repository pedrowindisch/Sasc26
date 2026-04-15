using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public class AttendanceService : IAttendanceService
{
    private static readonly TimeZoneInfo BrasiliaTz = GetBrasiliaTimeZone();

    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly EventSettings _settings;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        AppDbContext db,
        IEmailService emailService,
        IOptions<EventSettings> settings,
        ILogger<AttendanceService> logger)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Session?> GetActiveSessionAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _logger.LogDebug("Current Brasília time: {Time}", now);
        return await _db.Sessions.FirstOrDefaultAsync(s => now >= s.StartTime && now <= s.EndTime);
    }

    public async Task<AttendeeProfileDto?> GetProfileAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var attendee = await _db.Attendees.FindAsync(email);
        if (attendee is null) return null;

        return new AttendeeProfileDto
        {
            Email = attendee.Email,
            FullName = attendee.FullName,
            Course = attendee.Course,
            Shift = attendee.Shift,
            Phase = attendee.Phase,
            Found = true
        };
    }

    public async Task<RequestOtpResult> RequestOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        if (!email.EndsWith($"@{_settings.AllowedEmailDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = $"Utilize seu e-mail institucional @{_settings.AllowedEmailDomain}."
            };
        }

        var session = await GetActiveSessionAsync();
        if (session is null)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = "Não há nenhuma sessão ativa no momento. Tente novamente dentro do horário do evento."
            };
        }

        var alreadyVerified = await _db.CheckIns
            .AnyAsync(c => c.AttendeeEmail == email && c.SessionId == session.Id && c.Status == CheckInStatus.Verified);
        if (alreadyVerified)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = "Sua presença nesta sessão já foi registrada. Caso precise alterar seus dados, espere a próxima sessão ou contate a organização."
            };
        }

        var attemptCount = await _db.CheckIns
            .CountAsync(c => c.AttendeeEmail == email && c.SessionId == session.Id);
        if (attemptCount >= _settings.MaxOtpAttemptsPerSession)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = $"Limite de {_settings.MaxOtpAttemptsPerSession} tentativas atingido para esta sessão."
            };
        }

        var existingAttendee = await _db.Attendees.FindAsync(email);
        if (existingAttendee is null)
        {
            _db.Attendees.Add(new Attendee
            {
                Email = email,
                FullName = string.Empty,
                Course = string.Empty,
                Shift = string.Empty,
                Phase = 0
            });
        }

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = email,
            SessionId = session.Id,
            OtpCode = otpCode,
            Status = CheckInStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_settings.OtpExpirationMinutes)
        };

        _db.CheckIns.Add(checkIn);
        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpEmailAsync(email, otpCode, session.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
            return new RequestOtpResult
            {
                Success = false,
                Message = "Erro ao enviar o e-mail. Tente novamente em instantes."
            };
        }

        return new RequestOtpResult
        {
            Success = true,
            Message = $"Código enviado para {email}. Verifique sua caixa de entrada."
        };
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var code = dto.Code.Trim();

        var session = await GetActiveSessionAsync();
        if (session is null)
        {
            return new VerifyOtpResult
            {
                Success = false,
                Message = "Não há nenhuma sessão ativa no momento."
            };
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        var checkIn = await _db.CheckIns
            .FirstOrDefaultAsync(c =>
                c.AttendeeEmail == email &&
                c.SessionId == session.Id &&
                c.OtpCode == code &&
                c.Status == CheckInStatus.Pending);

        if (checkIn is null)
        {
            return new VerifyOtpResult
            {
                Success = false,
                Message = "Código inválido. Verifique e tente novamente."
            };
        }

        if (now > checkIn.ExpiresAt)
        {
            return new VerifyOtpResult
            {
                Success = false,
                Message = "O código expirou. Solicite um novo código."
            };
        }

        var existingAttendee = await _db.Attendees.FindAsync(email);
        if (existingAttendee is not null)
        {
            existingAttendee.FullName = dto.FullName.Trim();
            existingAttendee.Course = dto.Course;
            existingAttendee.Shift = dto.Shift;
            existingAttendee.Phase = dto.Phase;
        }
        else
        {
            _db.Attendees.Add(new Attendee
            {
                Email = email,
                FullName = dto.FullName.Trim(),
                Course = dto.Course,
                Shift = dto.Shift,
                Phase = dto.Phase
            });
        }

        checkIn.Status = CheckInStatus.Verified;
        checkIn.VerifiedAt = now;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Check-in verified for {Email} in session {Session}", email, session.Name);

        return new VerifyOtpResult
        {
            Success = true,
            Message = $"Presença registrada com sucesso em \"{session.Name}\"!"
        };
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
