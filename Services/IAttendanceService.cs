using Sasc26.Models;

namespace Sasc26.Services;

public interface IAttendanceService
{
    Task<Session?> GetActiveSessionAsync();
    Task<AttendeeProfileDto?> GetProfileAsync(string email);
    Task<RequestOtpResult> RequestOtpAsync(string email);
    Task<VerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto);
}

public class RequestOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerifyOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
