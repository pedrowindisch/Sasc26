using Sasc26.Models;

namespace Sasc26.Services;

public interface IAttendanceService
{
    Task<TimeSlot?> GetActiveTimeSlotAsync();
    Task<AttendeeProfileDto?> GetProfileAsync(string email);
    Task<RequestOtpResult> RequestOtpAsync(string email);
    Task<VerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto);
    Task<List<LectureDto>> GetActiveLecturesAsync();
    Task<List<LectureWithPreRegDto>> GetAllLecturesAsync();
    Task<PreRegisterResult> SubmitPreRegistrationBatchAsync(string email, List<int> lectureIds);
    Task<PreRegisterResult> VerifyPreRegistrationOtpAsync(string email, string code);
    Task<HashSet<int>> GetPreRegisteredLectureIdsAsync(string email);
    Task<SubmitCheckInResult> SubmitCheckInAsync(SubmitCheckInDto dto);
    Task<List<RetroactiveLectureDto>> GetYesterdayLecturesAsync();
    Task<RetroactiveRequestResult> SubmitRetroactiveRequestAsync(RetroactiveRequestDto dto);
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

public class PreRegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LectureWithPreRegDto
{
    public int Id { get; set; }
    public int TimeSlotId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string TimeSlotLabel { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public bool IsPreRegistrationEnabled { get; set; }
    public int PreRegistrationCount { get; set; }
    public bool AlreadyRegistered { get; set; }
}
