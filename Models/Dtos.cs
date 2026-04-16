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
