using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RentCar.Domain.Entities;

namespace RentCar.Application.Abstractions;

/// <summary>Hợp đồng truy cập dữ liệu mà tầng Application cần. Được cài đặt bởi AppDbContext ở tầng Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<DriverLicense> DriverLicenses { get; }
    DbSet<VehicleType> VehicleTypes { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<VehicleImage> VehicleImages { get; }
    DbSet<PickupLocation> PickupLocations { get; }
    DbSet<PriceRule> PriceRules { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionVehicle> PromotionVehicles { get; }
    DbSet<SurchargePolicy> SurchargePolicies { get; }
    DbSet<RentalRequest> RentalRequests { get; }
    DbSet<RentalContract> Contracts { get; }
    DbSet<ContractLine> ContractLines { get; }
    DbSet<ContractHistory> ContractHistories { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<HandoverRecord> Handovers { get; }
    DbSet<HandoverImage> HandoverImages { get; }
    DbSet<StaffInvitation> StaffInvitations { get; }
    DbSet<MaintenanceSchedule> MaintenanceSchedules { get; }

    /// <summary>Gọi hàm dbo.fnXeKhaDung: mã các xe khả dụng trong [startAt, endAt) (FR-2.1, BR-01).</summary>
    Task<List<int>> AvailableVehicleIdsAsync(DateTime startAt, DateTime endAt, CancellationToken ct = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Thực thi lệnh SQL có tham số trong giao dịch hiện tại (dùng để khóa dòng khi ghi nhận đặt cọc, #12).</summary>
    Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Băm và kiểm tra mật khẩu bằng PBKDF2 (NFR-02). Cài đặt ở Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string password);
}

/// <summary>
/// Gửi email (FR-6.1c). Cài đặt SMTP ở Infrastructure; NullEmailSender khi chưa cấu hình SMTP.
/// Không bao giờ ghi nội dung email hay mã vào log.
/// </summary>
public interface IEmailSender
{
    /// <summary>true khi không gửi thật (chế độ mô phỏng, chưa cấu hình SMTP).</summary>
    bool IsSimulated { get; }

    /// <summary>Trả về true nếu gửi thành công hoặc đã ghi nhận ở chế độ mô phỏng; false nếu gửi lỗi.</summary>
    Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
