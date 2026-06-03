using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailService(
    IOptions<EmailSettings> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _cfg = options.Value;

    public async Task SendOTPToEmailAsync(string toEmail, string toName, tring otp, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Mã xác thực OTP";

        var builder = new BodyBuilder
        {
            TextBody = $"Mã xác thực OTP của bạn là: {otp} có hiệu lực trong 5 phút.",
            HtmlBody = BuildHtml(otp)
        };
        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            var secureOption = _cfg.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(_cfg.Host, _cfg.Port, secureOption, ct);
            await smtp.AuthenticateAsync(_cfg.UserName, _cfg.Password, ct);
            await smtp.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw; // để caller xử lý hoặc bubble lên global handler
        }
        finally
        {
            await smtp.DisconnectAsync(quit: true, ct);
        }
    }

    private static string BuildHtml(string otp) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:sans-serif;background:#f4f4f5;margin:0;padding:32px">
          <div style="max-width:480px;margin:auto;background:#fff;
                      border-radius:12px;padding:40px;border:1px solid #e4e4e7">
            <h2 style="margin-top:0;font-size:20px;color:#18181b">Xác thực tài khoản</h2>
            <p style="color:#52525b;font-size:14px;line-height:1.6">
              Sử dụng mã OTP bên dưới để hoàn tất đăng ký.
              Mã có hiệu lực trong <strong>5 phút</strong>.
            </p>
            <div style="margin:28px 0;text-align:center">
              <span style="display:inline-block;letter-spacing:10px;font-size:36px;
                           font-weight:700;color:#18181b;background:#f4f4f5;
                           padding:16px 28px;border-radius:8px">{otp}</span>
            </div>
            <p style="color:#a1a1aa;font-size:12px;margin-bottom:0">
              Nếu bạn không yêu cầu mã này, hãy bỏ qua email này.
            </p>
          </div>
        </body>
        </html>
        """;
}