using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

/// <summary>
/// Super admin controller for managing events (create, edit, list).
/// Accessible at /SuperAdmin (no event slug required).
/// Only users whose email is in the global AdminEmails config can access.
/// </summary>
public class SuperAdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<SuperAdminController> _logger;
    private readonly EventSettings _settings;

    private const string SuperAdminSessionKey = "SuperAdminEmail";

    // In-memory OTP store for super admin login (separate from event-scoped admin OTPs)
    private static readonly Dictionary<string, (string Code, DateTime Expires)> _superAdminOtps = new();

    private bool IsSuperAdminLoggedIn =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString(SuperAdminSessionKey));

    public SuperAdminController(AppDbContext db, IEmailService emailService, IOptions<EventSettings> settings, ILogger<SuperAdminController> logger)
    {
        _db = db;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the given email is in the global admin list (from appsettings).
    /// Does NOT use IAdminService because that requires an event slug context.
    /// </summary>
    private bool IsGlobalAdminEmail(string email)
    {
        email = email.Trim().ToLowerInvariant();
        return _settings.AdminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Login page for super admin.
    /// </summary>
    public IActionResult Index()
    {
        if (IsSuperAdminLoggedIn)
            return RedirectToAction(nameof(List));

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendLoginOtp([FromBody] SuperAdminLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        var email = dto.Email.Trim().ToLowerInvariant();

        // Only allow global admin emails from appsettings
        if (!IsGlobalAdminEmail(email))
            return Json(new { success = false, message = "E-mail não autorizado." });

        var otpCode = Random.Shared.Next(100000, 999999).ToString("D6");
        _superAdminOtps[email] = (otpCode, DateTime.UtcNow.AddMinutes(10));

        try
        {
            await _emailService.SendOtpEmailAsync(email, otpCode, "Super Admin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send super admin OTP");
            _logger.LogWarning("===== SUPER ADMIN OTP FALLBACK ===== {Email} | Code: {Code}", email, otpCode);
        }

        return Json(new { success = true, message = $"Código enviado para {email}." });
    }

    [HttpPost]
    public IActionResult VerifyLoginOtp([FromBody] SuperAdminVerifyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
            return Json(new { success = false, message = "Informe o código." });

        var email = dto.Email.Trim().ToLowerInvariant();

        if (!_superAdminOtps.TryGetValue(email, out var otp))
            return Json(new { success = false, message = "Código inválido." });

        if (DateTime.UtcNow > otp.Expires)
        {
            _superAdminOtps.Remove(email);
            return Json(new { success = false, message = "Código expirado." });
        }

        if (otp.Code != dto.Code.Trim())
            return Json(new { success = false, message = "Código inválido." });

        _superAdminOtps.Remove(email);
        HttpContext.Session.SetString(SuperAdminSessionKey, email);

        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SuperAdminSessionKey);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// List all events (requires super admin auth).
    /// </summary>
    public async Task<IActionResult> List()
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        var events = await _db.Events.OrderBy(e => e.Name).ToListAsync();
        return View(events);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        return View(new Event());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Event ev, IFormFile? LogoFile, IFormFile? BackgroundDesktopFile, IFormFile? BackgroundMobileFile)
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        if (string.IsNullOrWhiteSpace(ev.Slug) || string.IsNullOrWhiteSpace(ev.Name))
        {
            ModelState.AddModelError("", "Slug e Nome são obrigatórios.");
            return View(ev);
        }

        if (await _db.Events.AnyAsync(e => e.Slug == ev.Slug))
        {
            ModelState.AddModelError("", "Já existe um evento com este slug.");
            return View(ev);
        }

        if (LogoFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await LogoFile.CopyToAsync(ms);
            ev.LogoImage = ms.ToArray();
            ev.LogoContentType = LogoFile.ContentType;
        }

        if (BackgroundDesktopFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await BackgroundDesktopFile.CopyToAsync(ms);
            ev.BackgroundImageDesktop = ms.ToArray();
            ev.BackgroundImageDesktopContentType = BackgroundDesktopFile.ContentType;
        }

        if (BackgroundMobileFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await BackgroundMobileFile.CopyToAsync(ms);
            ev.BackgroundImageMobile = ms.ToArray();
            ev.BackgroundImageMobileContentType = BackgroundMobileFile.ContentType;
        }

        ev.AdminEmailsJson = string.IsNullOrWhiteSpace(ev.AdminEmailsJson) ? "[]" : ev.AdminEmailsJson;
        ev.PostCheckinButtonsJson = string.IsNullOrWhiteSpace(ev.PostCheckinButtonsJson) ? "[]" : ev.PostCheckinButtonsJson;

        if ((ev.PreRegistrationStart is null) != (ev.PreRegistrationEnd is null))
        {
            ModelState.AddModelError("", "Os campos de período de pré-inscrição devem ser preenchidos juntos.");
            return View(ev);
        }

        // Save courses
        var coursesJson = Request.Form["CoursesJson"].ToString();
        if (!string.IsNullOrWhiteSpace(coursesJson))
        {
            var courses = System.Text.Json.JsonSerializer.Deserialize<List<CourseFormItem>>(coursesJson) ?? new();
            foreach (var c in courses)
            {
                if (!string.IsNullOrWhiteSpace(c.Name))
                {
                    _db.EventCourses.Add(new EventCourse
                    {
                        EventId = ev.Id,
                        Name = c.Name.Trim(),
                        NumberOfSemesters = c.NumberOfSemesters
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        var ev = await _db.Events.Include(e => e.Courses).FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound();
        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Event ev, IFormFile? LogoFile, bool removeLogo = false, IFormFile? BackgroundDesktopFile = null, bool removeBackgroundDesktop = false, IFormFile? BackgroundMobileFile = null, bool removeBackgroundMobile = false)
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        var existing = await _db.Events.FindAsync(ev.Id);
        if (existing is null) return NotFound();

        existing.Slug = ev.Slug;
        existing.Name = ev.Name;
        existing.Subtitle = ev.Subtitle;
        existing.AllowedEmailDomain = ev.AllowedEmailDomain;
        existing.AdminEmailsJson = ev.AdminEmailsJson;
        existing.PostCheckinButtonsJson = ev.PostCheckinButtonsJson;
        existing.CheckInMode = ev.CheckInMode;
        existing.RequireOtp = ev.RequireOtp;
        existing.IsRetroactiveCheckInEnabled = ev.IsRetroactiveCheckInEnabled;
        existing.PrimaryColor = ev.PrimaryColor;
        existing.AccentColor = ev.AccentColor;
        existing.BackgroundColor = ev.BackgroundColor;
        existing.TextColor = ev.TextColor;

        if (removeLogo)
        {
            existing.LogoImage = null;
            existing.LogoContentType = string.Empty;
        }
        else if (LogoFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await LogoFile.CopyToAsync(ms);
            existing.LogoImage = ms.ToArray();
            existing.LogoContentType = LogoFile.ContentType;
        }

        if (removeBackgroundDesktop)
        {
            existing.BackgroundImageDesktop = null;
            existing.BackgroundImageDesktopContentType = string.Empty;
        }
        else if (BackgroundDesktopFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await BackgroundDesktopFile.CopyToAsync(ms);
            existing.BackgroundImageDesktop = ms.ToArray();
            existing.BackgroundImageDesktopContentType = BackgroundDesktopFile.ContentType;
        }

        if (removeBackgroundMobile)
        {
            existing.BackgroundImageMobile = null;
            existing.BackgroundImageMobileContentType = string.Empty;
        }
        else if (BackgroundMobileFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await BackgroundMobileFile.CopyToAsync(ms);
            existing.BackgroundImageMobile = ms.ToArray();
            existing.BackgroundImageMobileContentType = BackgroundMobileFile.ContentType;
        }

        if ((ev.PreRegistrationStart is null) != (ev.PreRegistrationEnd is null))
        {
            ModelState.AddModelError("", "Os campos de período de pré-inscrição devem ser preenchidos juntos.");
            return View(ev);
        }
        existing.PreRegistrationStart = ev.PreRegistrationStart;
        existing.PreRegistrationEnd = ev.PreRegistrationEnd;

        // Sync courses
        var coursesJson = Request.Form["CoursesJson"].ToString();
        if (!string.IsNullOrWhiteSpace(coursesJson))
        {
            var incomingCourses = System.Text.Json.JsonSerializer.Deserialize<List<CourseFormItem>>(coursesJson) ?? new();
            var existingCourses = await _db.EventCourses.Where(c => c.EventId == existing.Id).ToListAsync();
            var incomingNames = incomingCourses.Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => c.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toDelete = existingCourses.Where(c => !incomingNames.Contains(c.Name)).ToList();
            _db.EventCourses.RemoveRange(toDelete);

            foreach (var c in incomingCourses)
            {
                if (string.IsNullOrWhiteSpace(c.Name)) continue;
                var match = existingCourses.FirstOrDefault(ec => ec.Name.Equals(c.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    match.Name = c.Name.Trim();
                    match.NumberOfSemesters = c.NumberOfSemesters;
                }
                else
                {
                    _db.EventCourses.Add(new EventCourse
                    {
                        EventId = existing.Id,
                        Name = c.Name.Trim(),
                        NumberOfSemesters = c.NumberOfSemesters
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsSuperAdminLoggedIn) return RedirectToAction(nameof(Index));

        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(List));
    }
}

public class SuperAdminLoginDto { public string Email { get; set; } = string.Empty; }
public class SuperAdminVerifyDto { public string Email { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }

public class CourseFormItem
{
    public string Name { get; set; } = string.Empty;
    public int NumberOfSemesters { get; set; } = 8;
}
