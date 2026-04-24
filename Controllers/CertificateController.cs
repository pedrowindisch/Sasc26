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
    private readonly AppDbContext _db;

    public CertificateController(ICertificateService certificateService, IFeedbackService feedbackService, AppDbContext db)
    {
        _certificateService = certificateService;
        _feedbackService = feedbackService;
        _db = db;
    }

    public IActionResult Index()
    {
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
    public async Task<IActionResult> BackgroundImage()
    {
        var config = await _db.CertificateConfigs.FirstOrDefaultAsync();
        if (config?.BackgroundImage is null || config.BackgroundImage.Length == 0)
            return NotFound();
        return File(config.BackgroundImage, config.BackgroundImageContentType ?? "image/png");
    }
}