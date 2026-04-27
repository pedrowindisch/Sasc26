using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface IAdminService
{
    bool IsAdminEmail(string email);
    Task<AdminOtpResult> SendAdminOtpAsync(string email);
    Task<AdminLoginResult> VerifyAdminOtpAsync(string email, string code);
    Task<ManualCheckInResult> ManualCheckInAsync(string email, string fullName, string course, string shift, int phase, int lectureId);
    Task<List<CheckInEntryDto>> GetAllCheckInsAsync(int? lectureId);
    Task<List<TimeSlot>> GetAllTimeSlotsAsync();
    Task<List<Lecture>> GetAllLecturesAsync();
    Task<Lecture> CreateLectureAsync(string title, string speaker, int timeSlotId);
    Task<bool> DeleteLectureAsync(int lectureId);
    Task<TimeSlot> CreateTimeSlotAsync(DateTime startTime, DateTime endTime, string shift, int creditHours = 2);
    Task<bool> UpdateTimeSlotCreditHoursAsync(int timeSlotId, int creditHours);
    Task<bool> DeleteTimeSlotAsync(int timeSlotId);
    Task TogglePreRegistrationAsync(int lectureId);
    Task<List<PreRegistrationEntryDto>> GetPreRegistrationsAsync(int lectureId);
    Task<Dictionary<int, int>> GetPreRegistrationCountsAsync();
    Task<List<RetroactiveRequestEntryDto>> GetPendingRetroactiveRequestsAsync();
    Task<ApproveRetroactiveResult> ApproveRetroactiveRequestAsync(Guid requestId);
    Task<RejectRetroactiveResult> RejectRetroactiveRequestAsync(Guid requestId);
}

public class AdminOtpResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class AdminLoginResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class ManualCheckInResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }

public class RetroactiveRequestEntryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string TimeSlotLabel { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

public class ApproveRetroactiveResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class RejectRetroactiveResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }

public class PreRegistrationEntryDto
{
    public string AttendeeEmail { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class CheckInEntryDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool SesFallback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class AdminService : IAdminService
{
    private static readonly TimeZoneInfo BrasiliaTz = GetBrasiliaTimeZone();

    private static readonly string[] WordBank =
    [
        "codigo", "nuvem", "teclado", "janela", "tela", "mouse", "dados",
        "sistema", "rede", "senha", "arquivo", "busca", "link", "site",
        "pixel", "byte", "cache", "debug", "fonte", "icone", "loop",
        "menu", "porta", "root", "servidor", "web", "algoritmo", "banco",
        "classe", "docker", "email", "git", "html", "java", "kernel",
        "linux", "navegador", "objeto", "programa", "query", "rust",
        "script", "token", "url", "virtual", "wifi", "xml", "yaml",
        "compilador", "frontend", "gateway", "hardware", "json",
        "backend", "login", "middleware", "pacote", "rota", "servico",
        "tabela", "versao", "bloco", "celula", "dado", "fila", "grafo",
        "indice", "lista", "mapa", "nodo", "pilha", "registro", "tupla"
    ];

    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly EventSettings _settings;
    private readonly IEventContext _eventContext;
    private readonly ILogger<AdminService> _logger;

    private static readonly Dictionary<string, (string Code, DateTime Expires)> _adminOtps = new();

    public AdminService(AppDbContext db, IEmailService emailService, IOptions<EventSettings> settings, IEventContext eventContext, ILogger<AdminService> logger)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings.Value;
        _eventContext = eventContext;
        _logger = logger;
    }

    private int EventId => _eventContext.CurrentEventId;
    private string EventName => _eventContext.CurrentEvent.Name;

    public bool IsAdminEmail(string email)
    {
        email = email.Trim().ToLowerInvariant();
        // Check per-event admins first
        var ev = _eventContext.CurrentEvent;
        if (ev.AdminEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            return true;
        // Fallback to global admin emails from config
        return _settings.AdminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AdminOtpResult> SendAdminOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        if (!IsAdminEmail(email))
            return new AdminOtpResult { Success = false, Message = "E-mail não autorizado." };

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        _adminOtps[email] = (otpCode, DateTime.UtcNow.AddMinutes(10));

        try { await _emailService.SendOtpEmailAsync(email, otpCode, $"Admin {EventName}"); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin OTP");
            _logger.LogWarning("===== ADMIN OTP FALLBACK ===== {Email} | Code: {Code}", email, otpCode);
        }

        return new AdminOtpResult { Success = true, Message = $"Código enviado para {email}." };
    }

    public Task<AdminLoginResult> VerifyAdminOtpAsync(string email, string code)
    {
        email = email.Trim().ToLowerInvariant();
        if (!_adminOtps.TryGetValue(email, out var otp))
            return Task.FromResult(new AdminLoginResult { Success = false, Message = "Código inválido." });

        if (DateTime.UtcNow > otp.Expires)
        {
            _adminOtps.Remove(email);
            return Task.FromResult(new AdminLoginResult { Success = false, Message = "Código expirado." });
        }

        if (otp.Code != code.Trim())
            return Task.FromResult(new AdminLoginResult { Success = false, Message = "Código inválido." });

        _adminOtps.Remove(email);
        return Task.FromResult(new AdminLoginResult { Success = true });
    }

    public async Task<ManualCheckInResult> ManualCheckInAsync(string email, string fullName, string course, string shift, int phase, int lectureId)
    {
        email = email.Trim().ToLowerInvariant();
        var lecture = await _db.Lectures.Include(l => l.TimeSlot).FirstOrDefaultAsync(l => l.EventId == EventId && l.Id == lectureId);
        if (lecture is null)
            return new ManualCheckInResult { Success = false, Message = "Palestra não encontrada." };

        var already = await _db.CheckIns.AnyAsync(c =>
            c.EventId == EventId &&
            c.AttendeeEmail == email &&
            c.Lecture != null && c.Lecture.TimeSlotId == lecture.TimeSlotId &&
            c.Status == CheckInStatus.Verified);
        if (already)
            return new ManualCheckInResult { Success = false, Message = "Check-in já existe para este horário." };

        var existing = await _db.Attendees.FirstOrDefaultAsync(a => a.EventId == EventId && a.Email == email);
        if (existing is not null)
        {
            existing.FullName = fullName.Trim();
            existing.Course = course;
            existing.Shift = shift;
            existing.Phase = phase;
        }
        else
        {
            _db.Attendees.Add(new Attendee { Email = email, EventId = EventId, FullName = fullName.Trim(), Course = course, Shift = shift, Phase = phase });
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = email,
            EventId = EventId,
            LectureId = lectureId,
            OtpCode = "MANUAL",
            Status = CheckInStatus.Verified,
            CreatedAt = now,
            ExpiresAt = now,
            VerifiedAt = now
        });

        await _db.SaveChangesAsync();
        return new ManualCheckInResult { Success = true, Message = $"Check-in manual para {email} em \"{lecture.Title}\"." };
    }

    public async Task<List<CheckInEntryDto>> GetAllCheckInsAsync(int? lectureId)
    {
        var query = _db.CheckIns
            .Include(c => c.Attendee)
            .Include(c => c.Lecture)
            .Where(c => c.EventId == EventId && c.Status == CheckInStatus.Verified)
            .AsQueryable();

        if (lectureId.HasValue)
            query = query.Where(c => c.LectureId == lectureId.Value);

        return await query.OrderByDescending(c => c.VerifiedAt)
            .Select(c => new CheckInEntryDto
            {
                Email = c.AttendeeEmail,
                FullName = c.Attendee.FullName,
                Course = c.Attendee.Course,
                Shift = c.Attendee.Shift,
                Phase = c.Attendee.Phase,
                LectureTitle = c.Lecture != null ? c.Lecture.Title : "N/A",
                Speaker = c.Lecture != null ? c.Lecture.Speaker : "",
                Status = c.OtpCode == "MANUAL" ? "Manual" : (c.SesFallback ? "Fallback" : "Verificado"),
                SesFallback = c.SesFallback,
                CreatedAt = c.CreatedAt,
                VerifiedAt = c.VerifiedAt
            }).ToListAsync();
    }

    public async Task<List<TimeSlot>> GetAllTimeSlotsAsync()
        => await _db.TimeSlots.Where(t => t.EventId == EventId).OrderBy(t => t.StartTime).ToListAsync();

    public async Task<List<Lecture>> GetAllLecturesAsync()
        => await _db.Lectures.Include(l => l.TimeSlot).Where(l => l.EventId == EventId).OrderBy(l => l.TimeSlot.StartTime).ThenBy(l => l.Title).ToListAsync();

    public async Task<Lecture> CreateLectureAsync(string title, string speaker, int timeSlotId)
    {
        var words = GenerateKeywords();
        var lecture = new Lecture
        {
            Title = title.Trim(),
            Speaker = speaker.Trim(),
            TimeSlotId = timeSlotId,
            EventId = EventId,
            Keyword1 = words[0],
            Keyword2 = words[1],
            Keyword3 = words[2]
        };
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();
        return lecture;
    }

    public async Task<bool> DeleteLectureAsync(int lectureId)
    {
        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.EventId == EventId && l.Id == lectureId);
        if (lecture is null) return false;
        _db.Lectures.Remove(lecture);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TimeSlot> CreateTimeSlotAsync(DateTime startTime, DateTime endTime, string shift, int creditHours = 2)
    {
        var ts = new TimeSlot { StartTime = startTime, EndTime = endTime, Shift = shift, CreditHours = creditHours, EventId = EventId };
        _db.TimeSlots.Add(ts);
        await _db.SaveChangesAsync();
        return ts;
    }

    public async Task<bool> UpdateTimeSlotCreditHoursAsync(int timeSlotId, int creditHours)
    {
        var ts = await _db.TimeSlots.FirstOrDefaultAsync(t => t.EventId == EventId && t.Id == timeSlotId);
        if (ts is null) return false;
        ts.CreditHours = creditHours;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTimeSlotAsync(int timeSlotId)
    {
        var ts = await _db.TimeSlots.FirstOrDefaultAsync(t => t.EventId == EventId && t.Id == timeSlotId);
        if (ts is null) return false;
        _db.TimeSlots.Remove(ts);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task TogglePreRegistrationAsync(int lectureId)
    {
        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.EventId == EventId && l.Id == lectureId);
        if (lecture is null) return;
        lecture.IsPreRegistrationEnabled = !lecture.IsPreRegistrationEnabled;
        await _db.SaveChangesAsync();
    }

    public async Task<List<PreRegistrationEntryDto>> GetPreRegistrationsAsync(int lectureId)
    {
        return await _db.PreRegistrations
            .Where(p => p.EventId == EventId && p.LectureId == lectureId && p.IsVerified)
            .OrderByDescending(p => p.RegisteredAt)
            .Select(p => new PreRegistrationEntryDto
            {
                AttendeeEmail = p.AttendeeEmail,
                RegisteredAt = p.RegisteredAt
            })
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetPreRegistrationCountsAsync()
    {
        return await _db.PreRegistrations
            .Where(p => p.EventId == EventId && p.IsVerified)
            .GroupBy(p => p.LectureId)
            .Select(g => new { LectureId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LectureId, x => x.Count);
    }

    public async Task<List<RetroactiveRequestEntryDto>> GetPendingRetroactiveRequestsAsync()
    {
        return await _db.RetroactiveCheckIns
            .Include(r => r.Attendee)
            .Include(r => r.Lecture)
                .ThenInclude(l => l.TimeSlot)
            .Where(r => r.EventId == EventId && r.Status == RetroactiveCheckInStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new RetroactiveRequestEntryDto
            {
                Id = r.Id,
                Email = r.AttendeeEmail,
                FullName = r.Attendee.FullName,
                Course = r.Attendee.Course,
                Shift = r.Attendee.Shift,
                Phase = r.Attendee.Phase,
                LectureId = r.LectureId,
                LectureTitle = r.Lecture.Title,
                Speaker = r.Lecture.Speaker,
                TimeSlotLabel = r.Lecture.TimeSlot.StartTime.ToString("dd/MM HH:mm") + " - " + r.Lecture.TimeSlot.EndTime.ToString("HH:mm"),
                Justification = r.Justification,
                RequestedAt = r.RequestedAt
            })
            .ToListAsync();
    }

    public async Task<ApproveRetroactiveResult> ApproveRetroactiveRequestAsync(Guid requestId)
    {
        var request = await _db.RetroactiveCheckIns
            .Include(r => r.Lecture)
                .ThenInclude(l => l.TimeSlot)
            .FirstOrDefaultAsync(r => r.EventId == EventId && r.Id == requestId);

        if (request is null)
            return new ApproveRetroactiveResult { Success = false, Message = "Solicitação não encontrada." };

        if (request.Status != RetroactiveCheckInStatus.Pending)
            return new ApproveRetroactiveResult { Success = false, Message = "Solicitação já foi processada." };

        var alreadyVerified = await _db.CheckIns
            .AnyAsync(c => c.EventId == EventId && c.AttendeeEmail == request.AttendeeEmail &&
                           c.Lecture != null &&
                           c.Lecture.TimeSlotId == request.Lecture.TimeSlotId &&
                           c.Status == CheckInStatus.Verified);
        if (alreadyVerified)
        {
            request.Status = RetroactiveCheckInStatus.Approved;
            request.ResolvedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
            await _db.SaveChangesAsync();
            return new ApproveRetroactiveResult { Success = false, Message = "O aluno já possui presença verificada neste horário. Solicitação descartada." };
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        _db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = request.AttendeeEmail,
            EventId = EventId,
            LectureId = request.LectureId,
            OtpCode = "RETROACTIVE",
            Status = CheckInStatus.Verified,
            CreatedAt = now,
            ExpiresAt = now,
            VerifiedAt = now
        });

        request.Status = RetroactiveCheckInStatus.Approved;
        request.ResolvedAt = now;

        await _db.SaveChangesAsync();

        return new ApproveRetroactiveResult { Success = true, Message = "Solicitação aprovada e presença registrada." };
    }

    public async Task<RejectRetroactiveResult> RejectRetroactiveRequestAsync(Guid requestId)
    {
        var request = await _db.RetroactiveCheckIns.FirstOrDefaultAsync(r => r.EventId == EventId && r.Id == requestId);
        if (request is null)
            return new RejectRetroactiveResult { Success = false, Message = "Solicitação não encontrada." };

        if (request.Status != RetroactiveCheckInStatus.Pending)
            return new RejectRetroactiveResult { Success = false, Message = "Solicitação já foi processada." };

        request.Status = RetroactiveCheckInStatus.Rejected;
        request.ResolvedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        await _db.SaveChangesAsync();

        return new RejectRetroactiveResult { Success = true, Message = "Solicitação rejeitada." };
    }

    private static string[] GenerateKeywords()
    {
        var shuffled = WordBank.OrderBy(_ => Random.Shared.Next()).ToArray();
        return [shuffled[0], shuffled[1], shuffled[2]];
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
