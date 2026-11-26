using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using RentCar.Application.Abstractions;

namespace RentCar.Infrastructure.Services;

/// <summary>
/// Cấu hình SMTP đọc từ mục Smtp của appsettings.json. Mật khẩu SMTP đặt bằng user-secrets (Smtp:Password),
/// không ghi trong mã nguồn hay appsettings.json.
/// </summary>
public sealed record SmtpSettings(string Host, int Port, bool EnableSsl, string From, string? UserName, string? Password);

/// <summary>
/// Gửi email qua System.Net.Mail (không thêm package ngoài). Email chỉ chứa mã, không chứa đường link.
/// Khi gửi lỗi chỉ ghi log chung (loại lỗi), không ghi địa chỉ, nội dung hay mã.
/// </summary>
public sealed class SmtpEmailSender(SmtpSettings settings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsSimulated => false;

    public async Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(settings.From, "RentCar"),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000
        };
        if (!string.IsNullOrWhiteSpace(settings.UserName))
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);

        try
        {
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Gửi email thất bại (loại lỗi: {ErrorType}).", ex.GetType().Name);
            return false;
        }
    }
}
