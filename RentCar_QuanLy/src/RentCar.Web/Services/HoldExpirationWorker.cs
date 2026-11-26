using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using RentCar.Infrastructure.Persistence;

namespace RentCar.Web.Services;

/// <summary>
/// #12 (FR-3.7, BR-15): tự hủy hợp đồng Chờ xác nhận quá hạn giữ chỗ mà chưa đặt cọc.
/// Logic nằm trong thủ tục dbo.spHuyHopDongQuaHanGiuCho (CSDL). Job SQL Server Agent là cách chính;
/// worker này là dự phòng cho máy không có Agent (Express, LocalDB). Cả hai cùng gọi một thủ tục idempotent,
/// nên chạy song song vẫn an toàn: lần chạy sau không tìm thấy hợp đồng nào để hủy nữa.
/// Tắt bằng HoldCleanup:Enabled = false trong appsettings khi đã có job Agent.
/// </summary>
public sealed class HoldExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<HoldExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section = configuration.GetSection("HoldCleanup");
        if (!section.GetValue("Enabled", true))
        {
            logger.LogInformation("Tự hủy hợp đồng quá hạn giữ chỗ đang tắt (HoldCleanup:Enabled = false).");
            return;
        }

        var intervalMinutes = Math.Clamp(section.GetValue("IntervalMinutes", 5), 1, 60);
        var systemUserId = Math.Max(1, section.GetValue("SystemUserId", 1));

        try
        {
            // Đợi ứng dụng khởi động xong rồi mới chạy lần đầu.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
            do
            {
                await RunOnceAsync(systemUserId, stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ứng dụng đang tắt: dừng bình thường.
        }
    }

    private async Task RunOnceAsync(int systemUserId, CancellationToken ct)
    {
        try
        {
            // Mỗi lần chạy dùng một scope riêng để DbContext không giữ kết nối giữa các lần.
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC dbo.spHuyHopDongQuaHanGiuCho @idNguoiHeThong = {systemUserId};", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Lỗi tạm thời (mất kết nối, chưa chạy script CSDL) không làm sập ứng dụng; lần sau sẽ thử lại.
            logger.LogError(ex, "Không chạy được spHuyHopDongQuaHanGiuCho; sẽ thử lại sau.");
        }
    }
}
