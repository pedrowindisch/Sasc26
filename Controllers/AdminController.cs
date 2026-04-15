using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class AdminController : Controller
{
    private const string AdminSessionKey = "AdminEmail";
    private readonly IAdminService _adminService;
    private readonly IAttendanceService _attendanceService;
    private readonly EventSettings _settings;

    public AdminController(IAdminService adminService, IAttendanceService attendanceService, IOptions<EventSettings> settings)
    {
        _adminService = adminService;
        _attendanceService = attendanceService;
        _settings = settings.Value;
    }

    private bool IsAdminLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString(AdminSessionKey));

    public IActionResult Index()
    {
        if (IsAdminLoggedIn)
            return RedirectToAction(nameof(Dashboard));
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
        if (!IsAdminLoggedIn)
            return RedirectToAction(nameof(Index));

        ViewBag.Sessions = await _adminService.GetAllSessionsAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ManualCheckIn([FromBody] ManualCheckInDto dto)
    {
        if (!IsAdminLoggedIn)
            return Json(new { success = false, message = "Não autenticado." });

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            return Json(new { success = false, message = "Preencha e-mail e nome." });

        var result = await _adminService.ManualCheckInAsync(dto.Email, dto.FullName, dto.Course ?? "", dto.Shift ?? "", dto.Phase, dto.SessionId);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetCheckIns(int? sessionId)
    {
        if (!IsAdminLoggedIn)
            return Json(new { success = false });

        var entries = await _adminService.GetAllCheckInsAsync(sessionId);
        return Json(new { success = true, entries });
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        return RedirectToAction(nameof(Index));
    }
}

public class AdminLoginDto
{
    public string Email { get; set; } = string.Empty;
}

public class AdminVerifyDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ManualCheckInDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Course { get; set; }
    public string? Shift { get; set; }
    public int Phase { get; set; }
    public int SessionId { get; set; }
}
