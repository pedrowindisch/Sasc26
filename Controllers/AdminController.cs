using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IVolunteerService _volunteerService;
    private readonly ICertificateService _certificateService;
    private readonly IFeedbackService _feedbackService;
    private readonly IThankYouService _thankYouService;
    private readonly IEventContext _eventContext;
    private readonly AppDbContext _db;
    private readonly EventSettings _settings;

    public AdminController(IAdminService adminService, IVolunteerService volunteerService, ICertificateService certificateService, IFeedbackService feedbackService, IThankYouService thankYouService, IEventContext eventContext, AppDbContext db, IOptions<EventSettings> settings)
    {
        _adminService = adminService;
        _volunteerService = volunteerService;
        _certificateService = certificateService;
        _feedbackService = feedbackService;
        _thankYouService = thankYouService;
        _eventContext = eventContext;
        _db = db;
        _settings = settings.Value;
    }

    private string AdminSessionKey => $"AdminEmail_{_eventContext.EventSlug}";
    private bool IsAdminLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString(AdminSessionKey));

    public IActionResult Index()
    {
        var slug = EventHelper.GetEventSlug(HttpContext);
        if (string.IsNullOrEmpty(slug))
        {
            var firstEvent = _db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).FirstOrDefault();
            if (firstEvent is not null)
                return Redirect($"/{firstEvent.Slug}/Admin");
            return NotFound("No events configured.");
        }
        if (IsAdminLoggedIn) return RedirectToAction(nameof(Dashboard));
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendLoginOtp([FromBody] AdminLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });
        var result = await _adminService.SendAdminOtpAsync(dto.Email);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] AdminVerifyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
            return Json(new { success = false, message = "Informe o código." });
        var result = await _adminService.VerifyAdminOtpAsync(dto.Email, dto.Code);
        if (result.Success)
            HttpContext.Session.SetString(AdminSessionKey, dto.Email.Trim().ToLowerInvariant());
        return Json(new { result.Success, result.Message });
    }

    public async Task<IActionResult> Dashboard()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        ViewBag.TimeSlots = await _adminService.GetAllTimeSlotsAsync();
        ViewBag.Lectures = await _adminService.GetAllLecturesAsync();
        ViewBag.PreRegCounts = await _adminService.GetPreRegistrationCountsAsync();
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ManualCheckIn([FromBody] ManualCheckInDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            return Json(new { success = false, message = "Preencha e-mail e nome." });
        var result = await _adminService.ManualCheckInAsync(dto.Email, dto.FullName, dto.Course ?? "", dto.Shift ?? "", dto.Phase, dto.LectureId);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetCheckIns(int? lectureId)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var entries = await _adminService.GetAllCheckInsAsync(lectureId);
        return Json(new { success = true, entries });
    }

    [HttpPost]
    public async Task<IActionResult> CreateLecture([FromBody] CreateLectureDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Speaker))
            return Json(new { success = false, message = "Preencha título e palestrante." });
        var lecture = await _adminService.CreateLectureAsync(dto.Title, dto.Speaker, dto.TimeSlotId);
        return Json(new { success = true, lecture });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLecture([FromBody] DeleteLectureDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var ok = await _adminService.DeleteLectureAsync(dto.LectureId);
        return Json(new { success = ok });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTimeSlot([FromBody] CreateTimeSlotDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var ts = await _adminService.CreateTimeSlotAsync(dto.StartTime, dto.EndTime, dto.Shift, dto.CreditHours);
        return Json(new { success = true, timeSlot = ts });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTimeSlot([FromBody] DeleteTimeSlotDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var ok = await _adminService.DeleteTimeSlotAsync(dto.TimeSlotId);
        return Json(new { success = ok });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTimeSlotCreditHours([FromBody] UpdateTimeSlotCreditHoursDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var ok = await _adminService.UpdateTimeSlotCreditHoursAsync(dto.TimeSlotId, dto.CreditHours);
        return Json(new { success = ok });
    }

    [HttpPost]
    public async Task<IActionResult> TogglePreRegistration([FromBody] TogglePreRegDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        await _adminService.TogglePreRegistrationAsync(dto.LectureId);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> PreRegistrations(int lectureId)
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var lecture = (await _adminService.GetAllLecturesAsync()).FirstOrDefault(l => l.Id == lectureId);
        if (lecture is null) return RedirectToAction(nameof(Dashboard));
        var entries = await _adminService.GetPreRegistrationsAsync(lectureId);
        ViewBag.Lecture = lecture;
        ViewBag.Entries = entries;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportPreRegistrationsCsv(int lectureId)
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var entries = await _adminService.GetPreRegistrationsAsync(lectureId);
        var lecture = (await _adminService.GetAllLecturesAsync()).FirstOrDefault(l => l.Id == lectureId);
        var csv = "Email,Data_Inscricao\n" + string.Join("\n", entries.Select(e => $"{e.AttendeeEmail},{e.RegisteredAt:yyyy-MM-dd HH:mm:ss}"));
        var fileName = $"inscricoes_{lecture?.Title?.Replace(" ", "_") ?? lectureId.ToString()}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    public async Task<IActionResult> Feedback()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var summaries = await _feedbackService.GetLectureFeedbackSummariesAsync();
        ViewBag.FeedbackSummaries = summaries;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetFeedbackSummaries()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var summaries = await _feedbackService.GetLectureFeedbackSummariesAsync();
        return Json(new { success = true, summaries });
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        return RedirectToAction(nameof(Index));
    }

    // Volunteer Admin Endpoints
    public async Task<IActionResult> Volunteers()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var volunteers = await _volunteerService.GetAllVolunteersAsync();
        var timeSlots = await _adminService.GetAllTimeSlotsAsync();
        ViewBag.Volunteers = volunteers;
        ViewBag.TimeSlots = timeSlots;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetVolunteers()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var volunteers = await _volunteerService.GetAllVolunteersAsync();
        return Json(new { success = true, volunteers });
    }

    [HttpGet]
    public async Task<IActionResult> GetVolunteerDetail(Guid id)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var volunteer = await _volunteerService.GetVolunteerByIdAsync(id);
        if (volunteer is null) return Json(new { success = false, message = "Voluntário não encontrado." });
        return Json(new { success = true, volunteer });
    }

    [HttpPost]
    public async Task<IActionResult> AddVolunteerCheckIn([FromBody] AdminVolunteerCheckInDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var result = await _volunteerService.AdminAddCheckInAsync(dto.VolunteerId, dto.TimeSlotId);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveVolunteerCheckIn([FromBody] AdminRemoveVolunteerCheckInDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var ok = await _volunteerService.AdminRemoveCheckInAsync(dto.CheckInId);
        return Json(new { success = ok });
    }

    // Certificate Config Endpoints
    public async Task<IActionResult> CertificateConfig()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var config = await _certificateService.GetConfigAsync();
        ViewBag.CertificateConfig = config;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCertificateConfig([FromBody] CertificateConfigDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var result = await _certificateService.UpdateConfigAsync(dto);
        return Json(new { success = true, templateMessage = result.TemplateMessage, titleColor = result.TitleColor, bodyColor = result.BodyColor, borderColor = result.BorderColor });
    }

    [HttpPost]
    public async Task<IActionResult> UploadCertificateBackground(IFormFile file)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        if (file is null || file.Length == 0) return Json(new { success = false, message = "Nenhum arquivo selecionado." });
        if (file.Length > 5 * 1024 * 1024) return Json(new { success = false, message = "Arquivo muito grande (máx 5MB)." });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        await _certificateService.UpdateBackgroundImageAsync(ms.ToArray(), file.ContentType);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCertificateBackground()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        await _certificateService.RemoveBackgroundImageAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetCertificateBackground()
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync(c => c.EventId == _eventContext.CurrentEventId);
        if (config?.BackgroundImage is null || config.BackgroundImage.Length == 0)
            return NotFound();
        return File(config.BackgroundImage, config.BackgroundImageContentType ?? "image/png");
    }

    public async Task<IActionResult> IssuedCertificates()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var certs = await _certificateService.GetAllIssuedCertificatesAsync();
        ViewBag.Certificates = certs;
        return View();
    }

    public async Task<IActionResult> ThankYouConfig()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var config = await _thankYouService.GetConfigAsync();
        ViewBag.ThankYouConfig = config;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateThankYouConfig([FromBody] ThankYouConfigDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var result = await _thankYouService.UpdateConfigAsync(dto);
        return Json(new { success = true, config = result });
    }

    public async Task<IActionResult> FormSubmissions()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var submissions = await _thankYouService.GetSubmissionsAsync();
        var tyConfig = await _thankYouService.GetConfigAsync();
        ViewBag.Submissions = submissions;
        ViewBag.FormFields = tyConfig.FormFields;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetFormSubmissions()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var submissions = await _thankYouService.GetSubmissionsAsync();
        return Json(new { success = true, submissions });
    }

    [HttpGet]
    public async Task<IActionResult> ExportFormSubmissionsCsv()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var submissions = await _thankYouService.GetSubmissionsAsync();
        var config = await _thankYouService.GetConfigAsync();
        var fieldLabels = config.FormFields.Select(f => f.Label).ToList();
        var header = "Email,Data_Envio," + string.Join(",", fieldLabels.Select(l => l.Replace(",", ";")));
        var rows = submissions.Select(s =>
        {
            var responses = string.Join(",", fieldLabels.Select(l =>
            {
                var resp = s.Responses.FirstOrDefault(r => r.Label == l);
                return $"\"{(resp?.Value ?? "").Replace("\"", "\"\"")}\"";
            }));
            return $"{s.AttendeeEmail},{s.SubmittedAt:yyyy-MM-dd HH:mm:ss},{responses}";
        });
        var csv = header + "\n" + string.Join("\n", rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "inscricoes_formulario.csv");
    }

    public async Task<IActionResult> RetroactiveRequests()
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var requests = await _adminService.GetPendingRetroactiveRequestsAsync();
        ViewBag.Requests = requests;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingRetroactiveRequests()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var requests = await _adminService.GetPendingRetroactiveRequestsAsync();
        return Json(new { success = true, requests });
    }

    [HttpPost]
    public async Task<IActionResult> ApproveRetroactiveRequest([FromBody] ApproveRetroactiveDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var result = await _adminService.ApproveRetroactiveRequestAsync(dto.RequestId);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> RejectRetroactiveRequest([FromBody] RejectRetroactiveDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var result = await _adminService.RejectRetroactiveRequestAsync(dto.RequestId);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetBanner()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false });
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.EventId == _eventContext.CurrentEventId);
        return Json(new { success = true, banner });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBanner([FromBody] UpdateBannerDto dto)
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var eventId = _eventContext.CurrentEventId;
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.EventId == eventId);
        if (banner is null)
        {
            banner = new Banner { EventId = eventId };
            _db.Banners.Add(banner);
        }
        banner.Title = dto.Title?.Trim() ?? string.Empty;
        banner.Description = dto.Description?.Trim() ?? string.Empty;
        banner.CtaText = dto.CtaText?.Trim() ?? string.Empty;
        banner.CtaUrl = dto.CtaUrl?.Trim() ?? string.Empty;
        banner.IsActive = dto.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Json(new { success = true, banner });
    }

    [HttpPost]
    public async Task<IActionResult> DeactivateBanner()
    {
        if (!IsAdminLoggedIn) return Json(new { success = false, message = "Não autenticado." });
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.EventId == _eventContext.CurrentEventId);
        if (banner is not null)
        {
            banner.IsActive = false;
            banner.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    public async Task<IActionResult> Projector(int lectureId)
    {
        var lecture = await _db.Lectures
            .Include(l => l.TimeSlot)
            .FirstOrDefaultAsync(l => l.EventId == _eventContext.CurrentEventId && l.Id == lectureId);
        if (lecture is null) return RedirectToAction("Index", "Home");
        return View(lecture);
    }

    [HttpGet]
    public async Task<IActionResult> MagicCheckInBroadcast(int lectureId)
    {
        if (!IsAdminLoggedIn) return RedirectToAction(nameof(Index));
        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.EventId == _eventContext.CurrentEventId && l.Id == lectureId);
        if (lecture is null) return RedirectToAction(nameof(Dashboard));

        var old = await _db.MagicCheckInSessions
            .Where(s => s.EventId == _eventContext.CurrentEventId && s.LectureId == lectureId && s.IsActive)
            .ToListAsync();
        old.ForEach(s => s.IsActive = false);

        var session = new MagicCheckInSession
        {
            Id = Guid.NewGuid(),
            LectureId = lectureId,
            EventId = _eventContext.CurrentEventId,
            Token = GenerateMagicToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsActive = true
        };
        _db.MagicCheckInSessions.Add(session);
        await _db.SaveChangesAsync();

        ViewBag.Lecture = lecture;
        ViewBag.Token = session.Token;
        ViewBag.Payload = $"{_eventContext.CurrentEvent.Name}:{lectureId}:{session.Token}";
        return View();
    }

    private static string GenerateMagicToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 12)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}

public class AdminLoginDto { public string Email { get; set; } = string.Empty; }
public class AdminVerifyDto { public string Email { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
public class ManualCheckInDto { public string Email { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Course { get; set; } public string? Shift { get; set; } public int Phase { get; set; } public int LectureId { get; set; } }
public class CreateLectureDto { public string Title { get; set; } = string.Empty; public string Speaker { get; set; } = string.Empty; public int TimeSlotId { get; set; } }
public class DeleteLectureDto { public int LectureId { get; set; } }
public class CreateTimeSlotDto { public DateTime StartTime { get; set; } public DateTime EndTime { get; set; } public string Shift { get; set; } = string.Empty; public int CreditHours { get; set; } = 2; }
public class UpdateTimeSlotCreditHoursDto { public int TimeSlotId { get; set; } public int CreditHours { get; set; } }
public class DeleteTimeSlotDto { public int TimeSlotId { get; set; } }
public class TogglePreRegDto { public int LectureId { get; set; } }
public class ApproveRetroactiveDto { public Guid RequestId { get; set; } }
public class RejectRetroactiveDto { public Guid RequestId { get; set; } }

public class UpdateBannerDto { public string Title { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string CtaText { get; set; } = string.Empty; public string CtaUrl { get; set; } = string.Empty; public bool IsActive { get; set; } }
