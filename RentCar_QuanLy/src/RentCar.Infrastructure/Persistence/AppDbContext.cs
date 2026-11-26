using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;

namespace RentCar.Infrastructure.Persistence;

/// <summary>DbContext ánh xạ vào CSDL RentCarDb có sẵn (script database/RentCarDb_BanChuan.sql), không tạo lại bảng bằng migration.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<DriverLicense> DriverLicenses => Set<DriverLicense>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<PickupLocation> PickupLocations => Set<PickupLocation>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionVehicle> PromotionVehicles => Set<PromotionVehicle>();
    public DbSet<SurchargePolicy> SurchargePolicies => Set<SurchargePolicy>();
    public DbSet<RentalRequest> RentalRequests => Set<RentalRequest>();
    public DbSet<RentalContract> Contracts => Set<RentalContract>();
    public DbSet<ContractLine> ContractLines => Set<ContractLine>();
    public DbSet<ContractHistory> ContractHistories => Set<ContractHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<HandoverRecord> Handovers => Set<HandoverRecord>();
    public DbSet<HandoverImage> HandoverImages => Set<HandoverImage>();
    public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();

    public async Task<List<int>> AvailableVehicleIdsAsync(DateTime startAt, DateTime endAt, CancellationToken ct = default) =>
        await Database.SqlQuery<int>($"SELECT idXe AS [Value] FROM dbo.fnXeKhaDung({VietnamTime.ToUtc(startAt)}, {VietnamTime.ToUtc(endAt)})")
            .ToListAsync(ct);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        await Database.BeginTransactionAsync(ct);

    public Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken ct = default) =>
        Database.ExecuteSqlAsync(sql, ct);

    /// <summary>
    /// Trigger trong CSDL dùng RAISERROR (mã 50000) cho vi phạm nghiệp vụ như chồng lịch xe (BR-01, BR-11).
    /// Chuyển thành lỗi 409 để tầng giao diện hiển thị đúng thông báo.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 50000 } sql)
        {
            throw new DomainException(sql.Message, 409);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        var vietnam = new ValueConverter<DateTime, DateTime>(v => VietnamTime.ToUtc(v), v => VietnamTime.FromUtc(v));
        var optional = new ValueConverter<DateTime?, DateTime?>(v => v.HasValue ? VietnamTime.ToUtc(v.Value) : v, v => v.HasValue ? VietnamTime.FromUtc(v.Value) : v);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                if (property.ClrType == typeof(DateTime)) property.SetValueConverter(vietnam);
                else if (property.ClrType == typeof(DateTime?)) property.SetValueConverter(optional);
    }
}
