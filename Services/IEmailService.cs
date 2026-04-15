namespace Sasc26.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otpCode, string sessionName);
}
