using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class AdminController : Controller
{
    private const string AdminSessionKey = "AdminEmail";
    private readonly IAdminService _adminService;
    private readonly EventSettings _settings;

    public AdminController(IAdminService adminService, IOptions<EventSettings> settings)
    {
        _adminService = adminService;
        _settings = settings.Value;
    }

    private bool IsAdminLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString(AdminSessionKey));

    public IActionResult Index()
    {
        if (IsAdminLoggedIn) return RedirectToAction(nameof(Dashboard));
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
        var ts = await _adminService.CreateTimeSlotAsync(dto.StartTime, dto.EndTime, dto.Shift);
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

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        return RedirectToAction(nameof(Index));
    }
}

public class AdminLoginDto { public string Email { get; set; } = string.Empty; }
public class AdminVerifyDto { public string Email { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
public class ManualCheckInDto { public string Email { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string? Course { get; set; } public string? Shift { get; set; } public int Phase { get; set; } public int LectureId { get; set; } }
public class CreateLectureDto { public string Title { get; set; } = string.Empty; public string Speaker { get; set; } = string.Empty; public int TimeSlotId { get; set; } }
public class DeleteLectureDto { public int LectureId { get; set; } }
public class CreateTimeSlotDto { public DateTime StartTime { get; set; } public DateTime EndTime { get; set; } public string Shift { get; set; } = string.Empty; }
public class DeleteTimeSlotDto { public int TimeSlotId { get; set; } }
public class TogglePreRegDto { public int LectureId { get; set; } }
