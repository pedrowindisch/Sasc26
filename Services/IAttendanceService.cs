using Sasc26.Models;

namespace Sasc26.Services;

public interface IAttendanceService
{
    Task<TimeSlot?> GetActiveTimeSlotAsync();
    Task<AttendeeProfileDto?> GetProfileAsync(string email);
    Task<RequestOtpResult> RequestOtpAsync(string email);
    Task<VerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto);
    Task<List<LectureDto>> GetActiveLecturesAsync();
    Task<SubmitCheckInResult> SubmitCheckInAsync(SubmitCheckInDto dto);
}

public class RequestOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool SesFallback { get; set; }
}

public class VerifyOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SubmitCheckInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
