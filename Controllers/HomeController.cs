using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAttendanceService _attendanceService;
    private readonly EventSettings _eventSettings;

    public HomeController(ILogger<HomeController> logger, IAttendanceService attendanceService, IOptions<EventSettings> eventSettings)
    {
        _logger = logger;
        _attendanceService = attendanceService;
        _eventSettings = eventSettings.Value;
    }

    public async Task<IActionResult> Index()
    {
        var session = await _attendanceService.GetActiveSessionAsync();
        ViewBag.ActiveSession = session?.Name;
        ViewBag.HasActiveSession = session is not null;
        ViewBag.InstagramUrl = _eventSettings.InstagramUrl;
        ViewBag.TshirtPresaleUrl = _eventSettings.TshirtPresaleUrl;
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

        return Json(new
        {
            found = true,
            fullName = profile.FullName,
            course = profile.Course,
            shift = profile.Shift,
            phase = profile.Phase
        });
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
