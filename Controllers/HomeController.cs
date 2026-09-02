using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAttendanceService _attendanceService;
    private readonly IPreRegistrationConfigService _preRegConfigService;
    private readonly IEventContext _eventContext;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public HomeController(ILogger<HomeController> logger, IAttendanceService attendanceService, IPreRegistrationConfigService preRegConfigService, IEventContext eventContext, AppDbContext db, IWebHostEnvironment env)
    {
        _logger = logger;
        _attendanceService = attendanceService;
        _preRegConfigService = preRegConfigService;
        _eventContext = eventContext;
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        // If no event slug in route, redirect to the first active event
        var slug = HttpContext.Request.RouteValues["eventSlug"] as string;
        if (string.IsNullOrEmpty(slug))
        {
            var firstEvent = await _db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).FirstOrDefaultAsync();
            if (firstEvent is not null)
            {
                return Redirect($"/{firstEvent.Slug}");
            }
            return NotFound("No events configured.");
        }

        var ev = _eventContext.CurrentEvent;
        var timeSlot = await _attendanceService.GetActiveTimeSlotAsync();
        ViewBag.ActiveTimeSlot = timeSlot;
        ViewBag.HasActiveTimeSlot = timeSlot is not null;
        ViewBag.Event = ev;
        ViewBag.CheckInMode = (int)ev.CheckInMode;
        ViewBag.RequireOtp = ev.RequireOtp;
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.EventId == ev.Id && b.IsActive);
        ViewBag.Banner = banner;

        var hasOpenPreRegistration = ev.IsEventWidePreRegistration
            || await _db.Lectures.AnyAsync(l => l.EventId == ev.Id && l.IsPreRegistrationEnabled);

        var scheduleEnabled = false;
        var scheduleDisabledReason = "";

        if (!hasOpenPreRegistration)
        {
            scheduleDisabledReason = "Não há palestras com inscrições abertas no momento.";
        }
        else if (ev.PreRegistrationStart is null || ev.PreRegistrationEnd is null)
        {
            scheduleDisabledReason = "O período de pré-inscrição não foi definido.";
        }
        else
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
            if (now >= ev.PreRegistrationStart && now <= ev.PreRegistrationEnd)
            {
                scheduleEnabled = true;
            }
            else
            {
                scheduleDisabledReason = "Fora do período de pré-inscrição.";
            }
        }

        ViewBag.ScheduleEnabled = scheduleEnabled;
        ViewBag.ScheduleDisabledReason = scheduleDisabledReason;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Json(null);

        var profile = await _attendanceService.GetProfileAsync(email);
        if (profile is null)
            return Json(new { found = false });

        return Json(new { found = true, fullName = profile.FullName, course = profile.Course, shift = profile.Shift, phase = profile.Phase });
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveLectures()
    {
        var lectures = await _attendanceService.GetActiveLecturesAsync();
        return Json(new { success = true, lectures });
    }

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var lectures = await _attendanceService.GetAllLecturesAsync();
        ViewBag.Event = _eventContext.CurrentEvent;
        return View(lectures);
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedule(string? email)
    {
        var lectures = await _attendanceService.GetAllLecturesAsync();
        var isEventWide = lectures.FirstOrDefault()?.IsEventWide ?? false;
        var hasEventWideRegistration = false;

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLowerInvariant();
            if (isEventWide)
            {
                hasEventWideRegistration = await _attendanceService.HasEventWideRegistrationAsync(email);
            }
            else
            {
                var registeredIds = await _attendanceService.GetPreRegisteredLectureIdsAsync(email);
                foreach (var l in lectures)
                    l.AlreadyRegistered = registeredIds.Contains(l.Id);
            }
        }
        return Json(new { success = true, lectures, isEventWide, hasEventWideRegistration });
    }

    [HttpPost]
    public async Task<IActionResult> PreRegister([FromBody] PreRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe seu e-mail." });
        if (dto.LectureIds == null || dto.LectureIds.Count == 0)
            return Json(new { success = false, message = "Selecione pelo menos uma palestra." });
        var result = await _attendanceService.SubmitPreRegistrationBatchAsync(dto.Email, dto.LectureIds);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyPreRegistration([FromBody] VerifyPreRegDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "E-mail não informado." });
        if (string.IsNullOrWhiteSpace(dto.Code))
            return Json(new { success = false, message = "Informe o código." });
        var result = await _attendanceService.VerifyPreRegistrationOtpAsync(dto.Email, dto.Code);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> PreRegisterEvent([FromBody] EventWidePreRegDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe seu e-mail." });
        var result = await _attendanceService.SubmitEventPreRegistrationAsync(dto.Email);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEventPreRegistration([FromBody] VerifyPreRegDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "E-mail não informado." });
        if (string.IsNullOrWhiteSpace(dto.Code))
            return Json(new { success = false, message = "Informe o código." });
        var result = await _attendanceService.VerifyEventPreRegistrationOtpAsync(dto.Email, dto.Code);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetPreRegistrationConfig(string? email)
    {
        var config = await _preRegConfigService.GetConfigAsync();

        object? profile = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var attendee = await _db.Attendees.FirstOrDefaultAsync(a => a.EventId == _eventContext.CurrentEventId && a.Email == email.Trim().ToLowerInvariant());
            if (attendee is not null)
            {
                profile = new
                {
                    fullName = attendee.FullName,
                    course = attendee.Course,
                    shift = attendee.Shift,
                    phase = attendee.Phase,
                    found = true
                };
            }
        }

        return Json(new { success = true, config, profile });
    }

    [HttpPost]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
    public async Task<IActionResult> SubmitPreRegistration()
    {
        SubmitPreRegistrationDto dto;
        Dictionary<int, IFormFile> fileMap = new();

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var email = form["email"].ToString();
            var lectureIdsJson = form["lectureIds"].ToString();
            var isEventWideStr = form["isEventWide"].ToString();
            var fullName = form["fullName"].ToString();
            var course = form["course"].ToString();
            var shift = form["shift"].ToString();
            var phaseStr = form["phase"].ToString();
            var formResponsesJson = form["formResponses"].ToString();

            List<int> lectureIds = [];
            try { lectureIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(lectureIdsJson) ?? []; } catch {}

            List<FormFieldResponseDto> formResponses = [];
            try { formResponses = System.Text.Json.JsonSerializer.Deserialize<List<FormFieldResponseDto>>(formResponsesJson) ?? []; } catch {}

            foreach (var key in form.Files.Select(f => f.Name))
            {
                if (key.StartsWith("file_") && int.TryParse(key.Substring(5), out var idx))
                {
                    var file = form.Files[key];
                    if (file != null) fileMap[idx] = file;
                }
            }

            dto = new SubmitPreRegistrationDto
            {
                Email = email,
                LectureIds = lectureIds,
                IsEventWide = bool.TryParse(isEventWideStr, out var b) && b,
                FullName = fullName,
                Course = course,
                Shift = shift,
                Phase = int.TryParse(phaseStr, out var p) ? p : 0,
                FormResponses = formResponses
            };

            if (fileMap.Count > 0)
            {
                var config = await _db.PreRegistrationConfigs.FirstOrDefaultAsync(c => c.EventId == _eventContext.CurrentEventId);
                List<FormFieldDto>? fields = null;
                if (config != null && !string.IsNullOrWhiteSpace(config.FormFields))
                {
                    try { fields = System.Text.Json.JsonSerializer.Deserialize<List<FormFieldDto>>(config.FormFields, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }); } catch { fields = []; }
                }

                var pendingFiles = new List<(int idx, Guid id, byte[] compressed, string fileName, string contentType, long originalSize, string label)>();
                foreach (var kv in fileMap)
                {
                    var idx = kv.Key;
                    var file = kv.Value;
                    var field = fields != null && idx >= 0 && idx < fields.Count ? fields[idx] : null;
                    if (field == null || field.Type != "arquivo")
                        return Json(new { success = false, message = $"Campo de arquivo inválido ({idx})." });
                    if (file.Length == 0)
                        return Json(new { success = false, message = $"Arquivo \"{field.Label}\" vazio." });
                    var maxBytes = Sasc26.Services.FormFileValidationHelper.GetMaxBytes(field.FileMaxSizeMb);
                    if (file.Length > maxBytes)
                        return Json(new { success = false, message = $"Arquivo \"{field.Label}\" excede {field.FileMaxSizeMb ?? 10}MB." });
                    if (!Sasc26.Services.FormFileValidationHelper.IsContentTypeAllowed(file.ContentType, field.FileAccept))
                        return Json(new { success = false, message = $"Tipo de arquivo não permitido para \"{field.Label}\"." });

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var raw = ms.ToArray();
                    var compressed = Sasc26.Services.FileCompressionHelper.GZipCompress(raw);
                    var guid = Guid.NewGuid();
                    pendingFiles.Add((idx, guid, compressed, file.FileName, file.ContentType ?? "application/octet-stream", file.Length, field.Label));
                }

                // validate required file fields without file
                if (fields != null)
                {
                    for (int i = 0; i < fields.Count; i++)
                    {
                        var f = fields[i];
                        if (f.Type == "arquivo" && f.Required && !fileMap.ContainsKey(i))
                            return Json(new { success = false, message = $"Selecione o arquivo \"{f.Label}\"." });
                    }
                }

                // inject guid into responses
                foreach (var pf in pendingFiles)
                {
                    if (pf.idx >= 0 && pf.idx < dto.FormResponses.Count)
                        dto.FormResponses[pf.idx].Value = pf.id.ToString();
                }

                // stash pending files in HttpContext for post-save linking
                HttpContext.Items["PendingPreRegFiles"] = pendingFiles;
            }
        }
        else
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return Json(new { success = false, message = "Corpo vazio." });
            dto = System.Text.Json.JsonSerializer.Deserialize<SubmitPreRegistrationDto>(body, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) ?? new SubmitPreRegistrationDto();
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe seu e-mail." });
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Json(new { success = false, message = "Informe seu nome completo." });
        if (string.IsNullOrWhiteSpace(dto.Course) || string.IsNullOrWhiteSpace(dto.Shift) || dto.Phase < 1)
            return Json(new { success = false, message = "Preencha todos os campos do cadastro." });

        var result = await _attendanceService.SubmitPreRegistrationWithFormAsync(dto);
        if (!result.Success) return Json(new { result.Success, result.Message });

        // link pending files to created submission
        if (HttpContext.Items["PendingPreRegFiles"] is List<(int idx, Guid id, byte[] compressed, string fileName, string contentType, long originalSize, string label)> pending)
        {
            var submission = await _db.PreRegistrationFormSubmissions
                .Where(s => s.EventId == _eventContext.CurrentEventId && s.AttendeeEmail == dto.Email.Trim().ToLowerInvariant())
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync();
            if (submission != null)
            {
                foreach (var pf in pending)
                {
                    _db.FormFiles.Add(new FormFileAttachment
                    {
                        Id = pf.id,
                        EventId = _eventContext.CurrentEventId,
                        PreRegistrationSubmissionId = submission.Id,
                        FieldLabel = pf.label,
                        FileName = pf.fileName,
                        ContentType = pf.contentType,
                        OriginalSize = pf.originalSize,
                        CompressedData = pf.compressed,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }
        }

        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe seu e-mail." });

        var result = await _attendanceService.RequestOtpAsync(dto.Email);
        return Json(new { result.Success, result.Message, result.SesFallback });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o código recebido." });

        if (!dto.SesFallback && string.IsNullOrWhiteSpace(dto.Code))
            return Json(new { success = false, message = "Informe o código recebido." });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Json(new { success = false, message = "Informe seu nome completo." });

        if (string.IsNullOrWhiteSpace(dto.Course) || string.IsNullOrWhiteSpace(dto.Shift) || dto.Phase < 1)
            return Json(new { success = false, message = "Preencha todos os campos do cadastro." });

        var result = await _attendanceService.VerifyOtpAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> SubmitCheckIn([FromBody] SubmitCheckInDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "E-mail não informado." });

        if (dto.LectureId <= 0)
            return Json(new { success = false, message = "Selecione uma palestra." });

        var ev = _eventContext.CurrentEvent;
        if (ev.CheckInMode == CheckInMode.Keywords && (string.IsNullOrWhiteSpace(dto.Keyword1) || string.IsNullOrWhiteSpace(dto.Keyword2) || string.IsNullOrWhiteSpace(dto.Keyword3)))
            return Json(new { success = false, message = "Preencha as 3 palavras-chave." });

        var result = await _attendanceService.SubmitCheckInAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> SubmitQrCheckIn([FromBody] QrCheckInDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "E-mail não informado." });
        if (string.IsNullOrWhiteSpace(dto.Token) || dto.LectureId <= 0)
            return Json(new { success = false, message = "QR Code inválido." });

        var result = await _attendanceService.QrCheckInAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public IActionResult RetroactiveCheckIn()
    {
        if (_eventContext.CurrentEvent.IsRetroactiveCheckInEnabled == false)
            return NotFound();

        var slug = EventHelper.GetEventSlug(HttpContext);
        if (string.IsNullOrEmpty(slug))
        {
            var firstEvent = _db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).FirstOrDefault();
            if (firstEvent is not null)
                return Redirect($"/{firstEvent.Slug}/Home/RetroactiveCheckIn");
            return NotFound("No events configured.");
        }
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetYesterdayLectures()
    {
        if (_eventContext.CurrentEvent.IsRetroactiveCheckInEnabled == false)
            return Json(new { success = false, message = "Recurso desabilitado." });

        var lectures = await _attendanceService.GetYesterdayLecturesAsync();
        return Json(new { success = true, lectures });
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRetroactiveCheckIn([FromBody] RetroactiveRequestDto dto)
    {
        if (_eventContext.CurrentEvent.IsRetroactiveCheckInEnabled == false)
            return Json(new { success = false, message = "Recurso desabilitado." });

        var result = await _attendanceService.SubmitRetroactiveRequestAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public IActionResult MagicCheckIn()
    {
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> MagicCheckIn([FromBody] MagicCheckInDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe seu e-mail." });
        if (string.IsNullOrWhiteSpace(dto.Token) || dto.LectureId <= 0)
            return Json(new { success = false, message = "Token inválido." });

        var result = await _attendanceService.MagicCheckInAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public IActionResult Logo()
    {
        var ev = _eventContext.CurrentEvent;
        if (ev.LogoImage is { Length: > 0 } && !string.IsNullOrEmpty(ev.LogoContentType))
        {
            return File(ev.LogoImage, ev.LogoContentType);
        }
        return PhysicalFile(Path.Combine(_env.WebRootPath, "dasc.svg"), "image/svg+xml");
    }

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public IActionResult Background(string type = "desktop")
    {
        var ev = _eventContext.CurrentEvent;
        if (type == "mobile" && ev.BackgroundImageMobile is { Length: > 0 } && !string.IsNullOrEmpty(ev.BackgroundImageMobileContentType))
        {
            return File(ev.BackgroundImageMobile, ev.BackgroundImageMobileContentType);
        }
        if (ev.BackgroundImageDesktop is { Length: > 0 } && !string.IsNullOrEmpty(ev.BackgroundImageDesktopContentType))
        {
            return File(ev.BackgroundImageDesktop, ev.BackgroundImageDesktopContentType);
        }
        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> QrScan([FromQuery] int lectureId, [FromQuery] string? token)
    {
        var ev = _eventContext.CurrentEvent;
        ViewBag.Event = ev;
        ViewBag.EventSlug = ev?.Slug;
        ViewBag.EventName = ev?.Name;
        ViewBag.LectureId = lectureId;
        ViewBag.Token = token ?? "";
        var lecture = await _db.Lectures.AsNoTracking()
            .FirstOrDefaultAsync(l => l.EventId == _eventContext.CurrentEventId && l.Id == lectureId);
        ViewBag.LectureTitle = lecture?.Title ?? "";
        ViewBag.Speaker = lecture?.Speaker ?? "";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var eventId = _eventContext.CurrentEventId;
        var courses = await _db.EventCourses
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Name, c.NumberOfSemesters })
            .ToListAsync();
        return Json(new { success = true, courses });
    }

    private static TimeZoneInfo BrasiliaTz => GetBrasiliaTimeZone();

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
