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
        var timeSlot = await _attendanceService.GetActiveTimeSlotAsync();
        ViewBag.ActiveTimeSlot = timeSlot;
        ViewBag.HasActiveTimeSlot = timeSlot is not null;
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
        return View(lectures);
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedule(string? email)
    {
        var lectures = await _attendanceService.GetAllLecturesAsync();
        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLowerInvariant();
            var registeredIds = await _attendanceService.GetPreRegisteredLectureIdsAsync(email);
            foreach (var l in lectures)
                l.AlreadyRegistered = registeredIds.Contains(l.Id);
        }
        return Json(new { success = true, lectures });
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

        if (string.IsNullOrWhiteSpace(dto.Keyword1) || string.IsNullOrWhiteSpace(dto.Keyword2) || string.IsNullOrWhiteSpace(dto.Keyword3))
            return Json(new { success = false, message = "Preencha as 3 palavras-chave." });

        var result = await _attendanceService.SubmitCheckInAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> RetroactiveCheckIn()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetYesterdayLectures()
    {
        var lectures = await _attendanceService.GetYesterdayLecturesAsync();
        return Json(new { success = true, lectures });
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRetroactiveCheckIn([FromBody] RetroactiveRequestDto dto)
    {
        var result = await _attendanceService.SubmitRetroactiveRequestAsync(dto);
        return Json(new { result.Success, result.Message });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
