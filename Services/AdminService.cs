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
}

public class AdminOtpResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class AdminLoginResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }
public class ManualCheckInResult { public bool Success { get; set; } public string Message { get; set; } = string.Empty; }

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
    private readonly ILogger<AdminService> _logger;

    private static readonly Dictionary<string, (string Code, DateTime Expires)> _adminOtps = new();

    public AdminService(AppDbContext db, IEmailService emailService, IOptions<EventSettings> settings, ILogger<AdminService> logger)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsAdminEmail(string email)
        => _settings.AdminEmails.Contains(email.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

    public async Task<AdminOtpResult> SendAdminOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        if (!IsAdminEmail(email))
            return new AdminOtpResult { Success = false, Message = "E-mail não autorizado." };

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        _adminOtps[email] = (otpCode, DateTime.UtcNow.AddMinutes(10));

        try { await _emailService.SendOtpEmailAsync(email, otpCode, "Admin SASC 26"); }
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
        var lecture = await _db.Lectures.Include(l => l.TimeSlot).FirstOrDefaultAsync(l => l.Id == lectureId);
        if (lecture is null)
            return new ManualCheckInResult { Success = false, Message = "Palestra não encontrada." };

        var already = await _db.CheckIns.AnyAsync(c =>
            c.AttendeeEmail == email &&
            c.Lecture != null && c.Lecture.TimeSlotId == lecture.TimeSlotId &&
            c.Status == CheckInStatus.Verified);
        if (already)
            return new ManualCheckInResult { Success = false, Message = "Check-in já existe para este horário." };

        var existing = await _db.Attendees.FindAsync(email);
        if (existing is not null)
        {
            existing.FullName = fullName.Trim();
            existing.Course = course;
            existing.Shift = shift;
            existing.Phase = phase;
        }
        else
        {
            _db.Attendees.Add(new Attendee { Email = email, FullName = fullName.Trim(), Course = course, Shift = shift, Phase = phase });
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = email,
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
            .Where(c => c.Status == CheckInStatus.Verified)
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
        => await _db.TimeSlots.OrderBy(t => t.StartTime).ToListAsync();

    public async Task<List<Lecture>> GetAllLecturesAsync()
        => await _db.Lectures.Include(l => l.TimeSlot).OrderBy(l => l.TimeSlot.StartTime).ThenBy(l => l.Title).ToListAsync();

    public async Task<Lecture> CreateLectureAsync(string title, string speaker, int timeSlotId)
    {
        var words = GenerateKeywords();
        var lecture = new Lecture
        {
            Title = title.Trim(),
            Speaker = speaker.Trim(),
            TimeSlotId = timeSlotId,
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
        var lecture = await _db.Lectures.FindAsync(lectureId);
        if (lecture is null) return false;
        _db.Lectures.Remove(lecture);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TimeSlot> CreateTimeSlotAsync(DateTime startTime, DateTime endTime, string shift, int creditHours = 2)
    {
        var ts = new TimeSlot { StartTime = startTime, EndTime = endTime, Shift = shift, CreditHours = creditHours };
        _db.TimeSlots.Add(ts);
        await _db.SaveChangesAsync();
        return ts;
    }

    public async Task<bool> UpdateTimeSlotCreditHoursAsync(int timeSlotId, int creditHours)
    {
        var ts = await _db.TimeSlots.FindAsync(timeSlotId);
        if (ts is null) return false;
        ts.CreditHours = creditHours;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTimeSlotAsync(int timeSlotId)
    {
        var ts = await _db.TimeSlots.FindAsync(timeSlotId);
        if (ts is null) return false;
        _db.TimeSlots.Remove(ts);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task TogglePreRegistrationAsync(int lectureId)
    {
        var lecture = await _db.Lectures.FindAsync(lectureId);
        if (lecture is null) return;
        lecture.IsPreRegistrationEnabled = !lecture.IsPreRegistrationEnabled;
        await _db.SaveChangesAsync();
    }

    public async Task<List<PreRegistrationEntryDto>> GetPreRegistrationsAsync(int lectureId)
    {
        return await _db.PreRegistrations
            .Where(p => p.LectureId == lectureId && p.IsVerified)
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
            .Where(p => p.IsVerified)
            .GroupBy(p => p.LectureId)
            .Select(g => new { LectureId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LectureId, x => x.Count);
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
