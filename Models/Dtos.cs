namespace Sasc26.Models;

public class RequestOtpDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool SesFallback { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
}

public class SubmitCheckInDto
{
    public string Email { get; set; } = string.Empty;
    public int LectureId { get; set; }
    public string Keyword1 { get; set; } = string.Empty;
    public string Keyword2 { get; set; } = string.Empty;
    public string Keyword3 { get; set; } = string.Empty;
}

public class AttendeeProfileDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public bool Found { get; set; }
}

public class LectureDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
}

public class PreRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public List<int> LectureIds { get; set; } = [];
}

public class VerifyPreRegDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VolunteerProfileDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Semester { get; set; }
}

public class VolunteerCheckInDto
{
    public string Email { get; set; } = string.Empty;
}

public class VolunteerDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Semester { get; set; }
    public bool IsVerified { get; set; }
    public DateTime RegisteredAt { get; set; }
    public List<VolunteerCheckInEntryDto> CheckIns { get; set; } = [];
}

public class VolunteerCheckInEntryDto
{
    public Guid Id { get; set; }
    public int TimeSlotId { get; set; }
    public string TimeSlotLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminVolunteerCheckInDto
{
    public Guid VolunteerId { get; set; }
    public int TimeSlotId { get; set; }
}

public class AdminRemoveVolunteerCheckInDto
{
    public Guid CheckInId { get; set; }
}

public class RetroactiveRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int Phase { get; set; }
    public int LectureId { get; set; }
    public string Justification { get; set; } = string.Empty;
}

public class RetroactiveLectureDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string TimeSlotLabel { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class RetroactiveRequestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CertificateRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
}

public class CertificateValidateDto
{
    public string Email { get; set; } = string.Empty;
    public string ValidationCode { get; set; } = string.Empty;
}

public class CertificateConfigDto
{
    public string TemplateMessage { get; set; } = string.Empty;
    public string TitleColor { get; set; } = "#113D76";
    public string BodyColor { get; set; } = "#1a1a1a";
    public string BorderColor { get; set; } = "#113D76";
    public bool HasBackgroundImage { get; set; }
}

public class IssuedCertificateDto
{
    public string ValidationCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public DateTime IssuedAt { get; set; }
}
