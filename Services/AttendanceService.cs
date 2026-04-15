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

    public async Task<TimeSlot?> GetActiveTimeSlotAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _logger.LogDebug("Current Brasília time: {Time}", now);
        return await _db.TimeSlots.FirstOrDefaultAsync(s => now >= s.StartTime && now <= s.EndTime);
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

        var timeSlot = await GetActiveTimeSlotAsync();
        if (timeSlot is null)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = "Não há nenhuma palestra acontecendo no momento."
            };
        }

        var alreadyVerified = await _db.CheckIns
            .AnyAsync(c => c.AttendeeEmail == email &&
                           c.Lecture != null &&
                           c.Lecture.TimeSlotId == timeSlot.Id &&
                           c.Status == CheckInStatus.Verified);
        if (alreadyVerified)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = "Sua presença neste horário já foi registrada."
            };
        }

        var pendingCount = await _db.CheckIns
            .CountAsync(c => c.AttendeeEmail == email && c.Status == CheckInStatus.Pending);
        if (pendingCount >= _settings.MaxOtpAttemptsPerSession)
        {
            return new RequestOtpResult
            {
                Success = false,
                Message = $"Limite de {_settings.MaxOtpAttemptsPerSession} tentativas atingido."
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

        _db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = email,
            OtpCode = otpCode,
            Status = CheckInStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_settings.OtpExpirationMinutes)
        });

        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpEmailAsync(email, otpCode, "SASC 26");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
            return new RequestOtpResult
            {
                Success = true,
                SesFallback = true,
                Message = $"Não foi possível enviar o código para {email}."
            };
        }

        return new RequestOtpResult
        {
            Success = true,
            Message = $"Código enviado para {email}."
        };
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        CheckIn? checkIn;

        if (dto.SesFallback)
        {
            checkIn = await _db.CheckIns
                .FirstOrDefaultAsync(c =>
                    c.AttendeeEmail == email &&
                    c.Status == CheckInStatus.Pending);
        }
        else
        {
            var code = dto.Code.Trim();
            checkIn = await _db.CheckIns
                .FirstOrDefaultAsync(c =>
                    c.AttendeeEmail == email &&
                    c.OtpCode == code &&
                    c.Status == CheckInStatus.Pending);

            if (checkIn is null)
                return new VerifyOtpResult { Success = false, Message = "Código inválido." };

            if (now > checkIn.ExpiresAt)
                return new VerifyOtpResult { Success = false, Message = "O código expirou." };
        }

        if (checkIn is null)
            return new VerifyOtpResult { Success = false, Message = "Nenhuma solicitação pendente." };

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

        checkIn.Status = CheckInStatus.OtpVerified;
        checkIn.SesFallback = dto.SesFallback;
        await _db.SaveChangesAsync();

        return new VerifyOtpResult { Success = true, Message = "E-mail verificado." };
    }

    public async Task<List<LectureDto>> GetActiveLecturesAsync()
    {
        var timeSlot = await GetActiveTimeSlotAsync();
        if (timeSlot is null) return [];

        return await _db.Lectures
            .Where(l => l.TimeSlotId == timeSlot.Id)
            .Select(l => new LectureDto
            {
                Id = l.Id,
                Title = l.Title,
                Speaker = l.Speaker
            })
            .ToListAsync();
    }

    public async Task<SubmitCheckInResult> SubmitCheckInAsync(SubmitCheckInDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var lecture = await _db.Lectures.FindAsync(dto.LectureId);
        if (lecture is null)
            return new SubmitCheckInResult { Success = false, Message = "Palestra não encontrada." };

        var timeSlot = await _db.TimeSlots.FindAsync(lecture.TimeSlotId);
        if (timeSlot is null)
            return new SubmitCheckInResult { Success = false, Message = "Horário não encontrado." };

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        if (now < timeSlot.StartTime || now > timeSlot.EndTime)
            return new SubmitCheckInResult { Success = false, Message = "Esta palestra não está acontecendo agora." };

        var alreadyVerified = await _db.CheckIns
            .AnyAsync(c => c.AttendeeEmail == email &&
                           c.Lecture != null &&
                           c.Lecture.TimeSlotId == timeSlot.Id &&
                           c.Status == CheckInStatus.Verified);
        if (alreadyVerified)
            return new SubmitCheckInResult { Success = false, Message = "Você já registrou presença neste horário." };

        var kw1 = dto.Keyword1.Trim().ToLowerInvariant();
        var kw2 = dto.Keyword2.Trim().ToLowerInvariant();
        var kw3 = dto.Keyword3.Trim().ToLowerInvariant();

        if (kw1 != lecture.Keyword1.ToLowerInvariant() ||
            kw2 != lecture.Keyword2.ToLowerInvariant() ||
            kw3 != lecture.Keyword3.ToLowerInvariant())
        {
            return new SubmitCheckInResult { Success = false, Message = "Palavras-chave incorretas. Verifique e tente novamente." };
        }

        var otpCheckIn = await _db.CheckIns
            .FirstOrDefaultAsync(c => c.AttendeeEmail == email && c.Status == CheckInStatus.OtpVerified);

        if (otpCheckIn is not null)
        {
            otpCheckIn.LectureId = lecture.Id;
            otpCheckIn.Status = CheckInStatus.Verified;
            otpCheckIn.VerifiedAt = now;
        }
        else
        {
            _db.CheckIns.Add(new CheckIn
            {
                Id = Guid.NewGuid(),
                AttendeeEmail = email,
                LectureId = lecture.Id,
                OtpCode = "DIRECT",
                Status = CheckInStatus.Verified,
                CreatedAt = now,
                ExpiresAt = now,
                VerifiedAt = now
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Check-in verified for {Email} in lecture {Lecture}", email, lecture.Title);

        return new SubmitCheckInResult
        {
            Success = true,
            Message = $"Presença registrada em \"{lecture.Title}\"!"
        };
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
