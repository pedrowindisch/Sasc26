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

    public async Task<List<LectureWithPreRegDto>> GetAllLecturesAsync()
    {
        return await _db.Lectures
            .Include(l => l.TimeSlot)
            .Include(l => l.PreRegistrations)
            .OrderBy(l => l.TimeSlot.StartTime)
            .ThenBy(l => l.Title)
            .Select(l => new LectureWithPreRegDto
            {
                Id = l.Id,
                TimeSlotId = l.TimeSlotId,
                Title = l.Title,
                Speaker = l.Speaker,
                TimeSlotLabel = l.TimeSlot.StartTime.ToString("dd/MM HH:mm") + " - " + l.TimeSlot.EndTime.ToString("HH:mm"),
                Shift = l.TimeSlot.Shift,
                Date = l.TimeSlot.StartTime.ToString("yyyy-MM-dd"),
                IsPreRegistrationEnabled = l.IsPreRegistrationEnabled,
                PreRegistrationCount = l.PreRegistrations.Count(p => p.IsVerified)
            })
            .ToListAsync();
    }

    public async Task<PreRegisterResult> SubmitPreRegistrationBatchAsync(string email, List<int> lectureIds)
    {
        email = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
            return new PreRegisterResult { Success = false, Message = "Informe seu e-mail." };

        if (lectureIds == null || lectureIds.Count == 0)
            return new PreRegisterResult { Success = false, Message = "Selecione pelo menos uma palestra." };

        var pending = await _db.PreRegistrations
            .Where(p => p.AttendeeEmail == email && !p.IsVerified)
            .ToListAsync();
        if (pending.Count > 0)
        {
            _db.PreRegistrations.RemoveRange(pending);
            await _db.SaveChangesAsync();
        }

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        var expiresAt = now.AddMinutes(_settings.OtpExpirationMinutes);
        var addedCount = 0;
        var usedTimeSlots = new HashSet<int>();

        foreach (var lectureId in lectureIds.Distinct())
        {
            var lecture = await _db.Lectures.FindAsync(lectureId);
            if (lecture is null || !lecture.IsPreRegistrationEnabled) continue;

            if (usedTimeSlots.Contains(lecture.TimeSlotId)) continue;
            usedTimeSlots.Add(lecture.TimeSlotId);

            var existingInSlot = await _db.PreRegistrations
                .FirstOrDefaultAsync(p => p.Lecture.TimeSlotId == lecture.TimeSlotId && p.AttendeeEmail == email && p.IsVerified);
            if (existingInSlot is not null)
                _db.PreRegistrations.Remove(existingInSlot);

            var sameLecture = await _db.PreRegistrations
                .AnyAsync(p => p.LectureId == lectureId && p.AttendeeEmail == email && p.IsVerified);
            if (sameLecture) continue;

            _db.PreRegistrations.Add(new PreRegistration
            {
                LectureId = lectureId,
                AttendeeEmail = email,
                RegisteredAt = now,
                OtpCode = otpCode,
                ExpiresAt = expiresAt,
                IsVerified = false
            });
            addedCount++;
        }

        if (addedCount == 0)
            return new PreRegisterResult { Success = false, Message = "Nenhuma palestra nova para inscrever." };

        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpEmailAsync(email, otpCode, "Inscrição SASC 26");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send pre-registration OTP to {Email}", email);
        }

        return new PreRegisterResult
        {
            Success = true,
            Message = $"Código enviado para {email}. Verifique sua caixa de entrada."
        };
    }

    public async Task<PreRegisterResult> VerifyPreRegistrationOtpAsync(string email, string code)
    {
        email = email.Trim().ToLowerInvariant();
        var codeTrimmed = code.Trim();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        var pending = await _db.PreRegistrations
            .Where(p => p.AttendeeEmail == email && !p.IsVerified && p.OtpCode == codeTrimmed)
            .ToListAsync();

        if (pending.Count == 0)
            return new PreRegisterResult { Success = false, Message = "Código inválido." };

        if (pending.Any(p => now > p.ExpiresAt))
            return new PreRegisterResult { Success = false, Message = "O código expirou. Tente novamente." };

        foreach (var p in pending)
            p.IsVerified = true;

        await _db.SaveChangesAsync();

        return new PreRegisterResult
        {
            Success = true,
            Message = $"Inscrição confirmada! Você se inscreveu em {pending.Count} palestra(s)."
        };
    }

    public async Task<HashSet<int>> GetPreRegisteredLectureIdsAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        return await _db.PreRegistrations
            .Where(p => p.AttendeeEmail == email && p.IsVerified)
            .Select(p => p.LectureId)
            .ToHashSetAsync();
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
