using RentCar.Application.Abstractions;

namespace RentCar.Infrastructure.Services;

/// <summary>
/// Chế độ mô phỏng khi chưa cấu hình Smtp:Host: không gửi email thật và không ghi nội dung ra đâu cả.
/// Ở môi trường Development, mã đặt lại được hiện trên màn hình để vẫn kiểm thử được (xem AccountController).
/// </summary>
public sealed class NullEmailSender : IEmailSender
{
    public bool IsSimulated => true;

    public Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default) =>
        Task.FromResult(true);
}
