public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string toName, string otp, CancellationToken ct = default);
}