using Microsoft.AspNetCore.Mvc;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class VolunteerController : Controller
{
    private const string VolunteerSessionKey = "VolunteerEmail";
    private readonly IVolunteerService _volunteerService;

    public VolunteerController(IVolunteerService volunteerService)
    {
        _volunteerService = volunteerService;
    }

    private bool IsVolunteerLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString(VolunteerSessionKey));

    public IActionResult Index()
    {
        if (IsVolunteerLoggedIn)
            return RedirectToAction(nameof(Dashboard));
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Lookup([FromBody] VolunteerProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        var result = await _volunteerService.LookupVolunteerAsync(dto.Email);
        return Json(new { result.Success, result.Message, result.Exists });
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] VolunteerProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Json(new { success = false, message = "Informe seu nome." });
        if (string.IsNullOrWhiteSpace(dto.Course) || string.IsNullOrWhiteSpace(dto.Shift) || dto.Semester < 1)
            return Json(new { success = false, message = "Preencha todos os campos." });

        var result = await _volunteerService.RegisterAsync(dto);
        if (result.Success)
            HttpContext.Session.SetString(VolunteerSessionKey, dto.Email.Trim().ToLowerInvariant());
        return Json(new { result.Success, result.Message });
    }

    [HttpPost]
    public IActionResult SetSession([FromBody] VolunteerProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false });
        HttpContext.Session.SetString(VolunteerSessionKey, dto.Email.Trim().ToLowerInvariant());
        return Json(new { success = true });
    }

    public async Task<IActionResult> Dashboard()
    {
        if (!IsVolunteerLoggedIn)
            return RedirectToAction(nameof(Index));

        var email = HttpContext.Session.GetString(VolunteerSessionKey)!;
        var dashboard = await _volunteerService.GetDashboardAsync(email);
        if (dashboard is null)
        {
            HttpContext.Session.Remove(VolunteerSessionKey);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Dashboard = dashboard;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn([FromBody] VolunteerCheckInDto _)
    {
        if (!IsVolunteerLoggedIn)
            return Json(new { success = false, message = "Voluntário não autenticado." });

        var email = HttpContext.Session.GetString(VolunteerSessionKey)!;
        var result = await _volunteerService.CheckInAsync(email);
        return Json(new { result.Success, result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        if (!IsVolunteerLoggedIn)
            return Json(new { success = false, message = "Voluntário não autenticado." });

        var email = HttpContext.Session.GetString(VolunteerSessionKey)!;
        var dashboard = await _volunteerService.GetDashboardAsync(email);
        if (dashboard is null)
            return Json(new { success = false, message = "Voluntário não encontrado." });

        return Json(new
        {
            success = true,
            name = dashboard.Name,
            email = dashboard.Email,
            activeTimeSlot = dashboard.ActiveTimeSlot,
            alreadyCheckedIn = dashboard.AlreadyCheckedInCurrentSlot,
            checkIns = dashboard.CheckIns
        });
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(VolunteerSessionKey);
        return RedirectToAction(nameof(Index));
    }
}