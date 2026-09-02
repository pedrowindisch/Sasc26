using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class CertificateController : Controller
{
    private readonly ICertificateService _certificateService;
    private readonly IFeedbackService _feedbackService;
    private readonly IThankYouService _thankYouService;
    private readonly IEventContext _eventContext;
    private readonly AppDbContext _db;

    public CertificateController(ICertificateService certificateService, IFeedbackService feedbackService, IThankYouService thankYouService, IEventContext eventContext, AppDbContext db)
    {
        _certificateService = certificateService;
        _feedbackService = feedbackService;
        _thankYouService = thankYouService;
        _eventContext = eventContext;
        _db = db;
    }

    public IActionResult Index()
    {
        var slug = EventHelper.GetEventSlug(HttpContext);
        if (string.IsNullOrEmpty(slug))
        {
            var firstEvent = _db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).FirstOrDefault();
            if (firstEvent is not null)
                return Redirect($"/{firstEvent.Slug}/Certificate");
            return NotFound("No events configured.");
        }
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Lookup([FromBody] CertificateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        var result = await _certificateService.LookupProfileAsync(dto.Email);
        return Json(new { result.Success, result.Message, result.Exists, result.Name, result.Course, result.Phase });
    }

    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] CertificateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Json(new { success = false, message = "Informe seu nome." });
        if (string.IsNullOrWhiteSpace(dto.Course))
            return Json(new { success = false, message = "Informe o curso." });
        if (string.IsNullOrWhiteSpace(dto.Phase))
            return Json(new { success = false, message = "Informe a fase." });

        var result = await _certificateService.IssueOrUpdateCertificateAsync(dto);
        return Json(new { result.Success, result.Message, result.ValidationCode });
    }

    [HttpGet]
    public async Task<IActionResult> Print(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction(nameof(Index));

        var result = await _certificateService.GetCertificateAsync(code);
        if (!result.Success)
            return RedirectToAction(nameof(Index));

        ViewBag.Certificate = result;
        return View();
    }

    public IActionResult Validate()
    {
        ViewBag.Event = _eventContext.CurrentEvent;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CheckFeedbackStatus([FromBody] CertificateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        var result = await _feedbackService.GetFeedbackStatusAsync(dto.Email);
        return Json(new { result.Success, result.Message, result.NeedsFeedback, result.Lectures });
    }

    [HttpPost]
    public async Task<IActionResult> DoValidate([FromBody] CertificateValidateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.ValidationCode))
            return Json(new { success = false, message = "Preencha e-mail e código." });

        var result = await _certificateService.ValidateCertificateAsync(dto.Email, dto.ValidationCode);
        return Json(new { result.Success, result.Message, result.Name, result.Email, result.TotalHours, result.IssuedAt });
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

    [HttpGet]
    public async Task<IActionResult> BackgroundImage()
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync(c => c.EventId == _eventContext.CurrentEventId);
        if (config?.BackgroundImage is null || config.BackgroundImage.Length == 0)
            return NotFound();
        return File(config.BackgroundImage, config.BackgroundImageContentType ?? "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> GetThankYouConfig()
    {
        var config = await _thankYouService.GetConfigAsync();
        return Json(new { success = true, config });
    }

    [HttpPost]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
    public async Task<IActionResult> SubmitForm()
    {
        SubmitFormDto dto;
        Dictionary<int, IFormFile> fileMap = new();

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var email = form["email"].ToString();
            var responsesJson = form["responses"].ToString();
            List<FormFieldResponseDto> responses = [];
            try { responses = System.Text.Json.JsonSerializer.Deserialize<List<FormFieldResponseDto>>(responsesJson, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) ?? []; } catch {}

            foreach (var f in form.Files)
            {
                if (f.Name.StartsWith("file_") && int.TryParse(f.Name.Substring(5), out var idx))
                    fileMap[idx] = f;
            }

            dto = new SubmitFormDto { Email = email, Responses = responses };

            if (fileMap.Count > 0)
            {
                var config = await _db.ThankYouConfigs.FirstOrDefaultAsync(c => c.EventId == _eventContext.CurrentEventId);
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
                    var compressed = Sasc26.Services.FileCompressionHelper.GZipCompress(ms.ToArray());
                    var guid = Guid.NewGuid();
                    pendingFiles.Add((idx, guid, compressed, file.FileName, file.ContentType ?? "application/octet-stream", file.Length, field.Label));
                }

                if (fields != null)
                {
                    for (int i = 0; i < fields.Count; i++)
                    {
                        var f = fields[i];
                        if (f.Type == "arquivo" && f.Required && !fileMap.ContainsKey(i))
                            return Json(new { success = false, message = $"Selecione o arquivo \"{f.Label}\"." });
                    }
                }

                foreach (var pf in pendingFiles)
                {
                    if (pf.idx >= 0 && pf.idx < dto.Responses.Count)
                        dto.Responses[pf.idx].Value = pf.id.ToString();
                }
                HttpContext.Items["PendingThankYouFiles"] = pendingFiles;
            }
        }
        else
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return Json(new { success = false, message = "Corpo vazio." });
            dto = System.Text.Json.JsonSerializer.Deserialize<SubmitFormDto>(body, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) ?? new SubmitFormDto();
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        await _thankYouService.SubmitFormAsync(dto);

        if (HttpContext.Items["PendingThankYouFiles"] is List<(int idx, Guid id, byte[] compressed, string fileName, string contentType, long originalSize, string label)> pendingTy)
        {
            var submission = await _db.FormSubmissions
                .Where(s => s.EventId == _eventContext.CurrentEventId && s.AttendeeEmail == dto.Email.Trim().ToLowerInvariant())
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync();
            if (submission != null)
            {
                foreach (var pf in pendingTy)
                {
                    _db.FormFiles.Add(new FormFileAttachment
                    {
                        Id = pf.id,
                        EventId = _eventContext.CurrentEventId,
                        FormSubmissionId = submission.Id,
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

        return Json(new { success = true });
    }
}
