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
    Task<ManualCheckInResult> ManualCheckInAsync(string email, string fullName, string course, string shift, int phase, int sessionId);
    Task<List<CheckInEntryDto>> GetAllCheckInsAsync(int? sessionId);
    Task<List<Session>> GetAllSessionsAsync();
}

public class AdminOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AdminLoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ManualCheckInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CheckInEntryDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool SesFallback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class AdminService : IAdminService
{
    private static readonly TimeZoneInfo BrasiliaTz = GetBrasiliaTimeZone();

    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly EventSettings _settings;
    private readonly ILogger<AdminService> _logger;

    private static Dictionary<string, (string Code, DateTime Expires)> _adminOtps = new();

    public AdminService(AppDbContext db, IEmailService emailService, IOptions<EventSettings> settings, ILogger<AdminService> logger)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsAdminEmail(string email)
    {
        return _settings.AdminEmails.Contains(email.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AdminOtpResult> SendAdminOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        if (!IsAdminEmail(email))
            return new AdminOtpResult { Success = false, Message = "E-mail não autorizado como administrador." };

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        var expires = DateTime.UtcNow.AddMinutes(10);

        _adminOtps[email] = (otpCode, expires);

        try
        {
            await _emailService.SendOtpEmailAsync(email, otpCode, "Admin SASC 26");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin OTP to {Email}", email);
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
        return Task.FromResult(new AdminLoginResult { Success = true, Message = "Autenticado com sucesso." });
    }

    public async Task<ManualCheckInResult> ManualCheckInAsync(string email, string fullName, string course, string shift, int phase, int sessionId)
    {
        email = email.Trim().ToLowerInvariant();

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session is null)
            return new ManualCheckInResult { Success = false, Message = "Sessão não encontrada." };

        var alreadyVerified = await _db.CheckIns
            .AnyAsync(c => c.AttendeeEmail == email && c.SessionId == sessionId && c.Status == CheckInStatus.Verified);
        if (alreadyVerified)
            return new ManualCheckInResult { Success = false, Message = "Este e-mail já possui check-in verificado nesta sessão." };

        var existingAttendee = await _db.Attendees.FindAsync(email);
        if (existingAttendee is not null)
        {
            existingAttendee.FullName = fullName.Trim();
            existingAttendee.Course = course;
            existingAttendee.Shift = shift;
            existingAttendee.Phase = phase;
        }
        else
        {
            _db.Attendees.Add(new Attendee
            {
                Email = email,
                FullName = fullName.Trim(),
                Course = course,
                Shift = shift,
                Phase = phase
            });
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            AttendeeEmail = email,
            SessionId = sessionId,
            OtpCode = "MANUAL",
            Status = CheckInStatus.Verified,
            SesFallback = false,
            CreatedAt = now,
            ExpiresAt = now,
            VerifiedAt = now
        });

        await _db.SaveChangesAsync();
        return new ManualCheckInResult { Success = true, Message = $"Check-in manual registrado para {email} em \"{session.Name}\"." };
    }

    public async Task<List<CheckInEntryDto>> GetAllCheckInsAsync(int? sessionId)
    {
        var query = _db.CheckIns
            .Include(c => c.Attendee)
            .Include(c => c.Session)
            .Where(c => c.Status == CheckInStatus.Verified)
            .AsQueryable();

        if (sessionId.HasValue)
            query = query.Where(c => c.SessionId == sessionId.Value);

        var brasiliaTz = BrasiliaTz;

        return await query
            .OrderByDescending(c => c.VerifiedAt)
            .Select(c => new CheckInEntryDto
            {
                Email = c.AttendeeEmail,
                FullName = c.Attendee.FullName,
                Course = c.Attendee.Course,
                Shift = c.Attendee.Shift,
                Phase = c.Attendee.Phase,
                SessionName = c.Session.Name,
                Status = c.SesFallback ? "SES Fallback" : "Verificado",
                SesFallback = c.SesFallback,
                CreatedAt = c.CreatedAt,
                VerifiedAt = c.VerifiedAt
            })
            .ToListAsync();
    }

    public async Task<List<Session>> GetAllSessionsAsync()
    {
        return await _db.Sessions.OrderBy(s => s.StartTime).ToListAsync();
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
