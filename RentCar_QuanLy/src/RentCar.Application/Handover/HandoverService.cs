using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;

namespace RentCar.Application.Handover;

public sealed record DeliveryRequest(
    int ContractId, int StaffId, DateTime DeliveredAt, int Km, int FuelPercent, string Condition,
    string? Accessories, bool IdentityAndVehicleChecked, IReadOnlyList<string> ImagePaths, IReadOnlyList<VehicleInspection>? Vehicles = null);

public sealed record ReturnRequest(
    int ContractId, int StaffId, DateTime ReturnedAt, int Km, int FuelPercent, string Condition,
    string? Accessories, decimal DamageCharge, bool NeedsMaintenance, IReadOnlyList<string> ImagePaths, IReadOnlyList<VehicleInspection>? Vehicles = null);

public sealed record VehicleInspection(int VehicleId, int Km, int FuelPercent, string Condition, decimal DamageCharge = 0, bool NeedsMaintenance = false);

public sealed record HandoverDesk(
    IReadOnlyList<RentalContract> ReadyForDelivery,
    IReadOnlyList<RentalContract> OnRent,
    IReadOnlyList<Vehicle> PendingInspection);

/// <summary>
/// UC-03: bàn giao và nhận lại xe (FR-4.1 đến FR-4.4) và thanh toán sau thuê (FR-5.1).
/// Mỗi thay đổi trạng thái ghi lịch sử hợp đồng và nhật ký kiểm toán trong cùng một giao dịch (NFR-09).
/// </summary>
public sealed class HandoverService(IAppDbContext db)
{
    private const int MaxImages = 10;

    public async Task<HandoverDesk> GetDeskAsync(CancellationToken ct = default)
    {
        var ready = await db.Contracts.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .Where(c => c.Status == ContractStatus.Confirmed)
            .OrderBy(c => c.StartAt)
            .ToListAsync(ct);

        var onRent = await db.Contracts.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .Include(c => c.Handover)
            .Where(c => c.Status == ContractStatus.Active)
            .OrderBy(c => c.EndAt)
            .ToListAsync(ct);

        var pending = await db.Vehicles.AsNoTracking()
            .Where(v => v.Status == VehicleStatus.PendingInspection || v.Status == VehicleStatus.Maintenance)
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

        return new HandoverDesk(ready, onRent, pending);
    }

    /// <summary>UC-03 bước 2 đến 4: đối chiếu, ghi biên bản giao và chuyển hợp đồng sang Đang thuê.</summary>
    public async Task<HandoverRecord> RecordDeliveryAsync(DeliveryRequest r, CancellationToken ct = default)
    {
        if (!r.IdentityAndVehicleChecked)
            throw new DomainException("Nhân viên phải đối chiếu giấy tờ khách và xe với hợp đồng trước khi bàn giao (A1).");
        ValidateInspection(r.Km, r.FuelPercent, r.Condition, r.ImagePaths);

        var now = VietnamTime.Now;
        if (r.DeliveredAt > now)
            throw new DomainException("Thời điểm bàn giao không được ở tương lai.");

        var contract = await LoadContractAsync(r.ContractId, ct);
        if (contract.Status != ContractStatus.Confirmed)
            throw new DomainException("Chỉ bàn giao xe cho hợp đồng đã xác nhận.", 409);
        if (contract.Lines.Count == 0 || contract.Lines.Any(l => l.Vehicle.Status != VehicleStatus.Ready))
            throw new DomainException("Xe không ở trạng thái Sẵn sàng nên không thể bàn giao (A1).", 409);

        var checks = ResolveChecks(contract, r.Vehicles, r.Km, r.FuelPercent, r.Condition);
        await using var tx = await db.BeginTransactionAsync(ct);

        var record = new HandoverRecord
        {
            ContractId = contract.Id,
            DeliveredAt = r.DeliveredAt,
            DeliveryKm = checks[0].Km,
            DeliveryChecksJson = JsonSerializer.Serialize(checks),
            DeliveryFuelPercent = checks[0].FuelPercent,
            DeliveryCondition = r.Condition.Trim(),
            DeliveryAccessories = Clean(r.Accessories),
            ExtraCharges = 0m
        };
        foreach (var path in r.ImagePaths)
            record.Images.Add(new HandoverImage { Path = path, Kind = HandoverImageKind.Delivery });
        db.Handovers.Add(record);

        contract.Status = ContractStatus.Active;
        contract.History.Add(new ContractHistory
        {
            FromStatus = ContractStatus.Confirmed,
            ToStatus = ContractStatus.Active,
            ChangedAt = now,
            ChangedById = r.StaffId,
            Note = "Bàn giao xe cho khách (UC-03)"
        });
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = r.StaffId,
            ObjectType = "HopDong",
            ObjectId = contract.Code,
            Action = "BAN_GIAO_XE",
            OldValue = "trangThai=1",
            NewValue = $"trangThai=2; km={r.Km}; nhienLieu={r.FuelPercent}%",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return record;
    }

    /// <summary>
    /// UC-03 bước 5 đến 7a: ghi km, nhiên liệu, tình trạng khi nhận lại; tính SP-01, SP-02 và SP-03;
    /// xe chuyển sang Chờ kiểm tra (hoặc Bảo trì, A3). Hợp đồng vẫn Đang thuê cho đến khi thu đủ.
    /// </summary>
    public async Task<HandoverRecord> RecordReturnAsync(ReturnRequest r, CancellationToken ct = default)
    {
        ValidateInspection(r.Km, r.FuelPercent, r.Condition, r.ImagePaths);

        var now = VietnamTime.Now;
        if (r.ReturnedAt > now)
            throw new DomainException("Thời điểm nhận lại không được ở tương lai.");

        var contract = await LoadContractAsync(r.ContractId, ct);
        if (contract.Status != ContractStatus.Active)
            throw new DomainException("Chỉ nhận lại xe của hợp đồng đang thuê.", 409);

        var record = contract.Handover
            ?? throw new DomainException("Hợp đồng chưa có biên bản bàn giao.", 409);
        if (record.ReturnedAt is not null)
            throw new DomainException("Xe của hợp đồng này đã được nhận lại.", 409);
        if (record.DeliveredAt is { } delivered && r.ReturnedAt <= delivered)
            throw new DomainException("Thời điểm nhận lại phải sau thời điểm bàn giao (BR-02).");
        var checks = ResolveChecks(contract, r.Vehicles, r.Km, r.FuelPercent, r.Condition, r.DamageCharge, r.NeedsMaintenance);
        var deliveredChecks = string.IsNullOrWhiteSpace(record.DeliveryChecksJson)
            ? (contract.Lines.Count == 1 ? new List<VehicleInspection> { new(contract.Lines[0].VehicleId, record.DeliveryKm ?? 0, record.DeliveryFuelPercent ?? 100, record.DeliveryCondition ?? "") } : [])
            : JsonSerializer.Deserialize<List<VehicleInspection>>(record.DeliveryChecksJson)!;
        if (deliveredChecks.Count != contract.Lines.Count) throw new DomainException("Biên bản giao chưa có đủ thông tin từng xe.", 409);
        decimal totalCharges = 0;
        foreach (var line in contract.Lines)
        {
            var check = checks.Single(x => x.VehicleId == line.VehicleId);
            var before = deliveredChecks.Single(x => x.VehicleId == line.VehicleId);
            if (check.Km < before.Km) throw new DomainException($"Số km nhận lại của {line.Vehicle.PlateNumber} nhỏ hơn lúc giao.");
            if (line.Vehicle.Status != VehicleStatus.Ready) throw new DomainException($"Xe {line.Vehicle.PlateNumber} không ở trạng thái đang cho thuê.", 409);
            var policy = await db.SurchargePolicies.AsNoTracking().FirstOrDefaultAsync(p => p.VehicleTypeId == line.Vehicle.VehicleTypeId, ct);
            totalCharges += SettlementRules.CalculateReturnCharges(contract.EndAt, r.ReturnedAt, line.DailyRateAtBooking,
                line.DayCount, check.Km - before.Km, check.DamageCharge,
                policy?.GraceMinutes ?? BookingRules.LateGraceMinutes,
                policy?.DailyKmLimit ?? BookingRules.KmPerDayAllowance,
                policy?.PricePerExtraKm ?? BookingRules.KmUnitPrice).Total;
        }

        await using var tx = await db.BeginTransactionAsync(ct);

        record.ReturnedAt = r.ReturnedAt;
        record.ReturnKm = checks[0].Km;
        record.ReturnChecksJson = JsonSerializer.Serialize(checks);
        record.ReturnFuelPercent = checks[0].FuelPercent;
        record.ReturnCondition = r.Condition.Trim();
        record.ReturnAccessories = Clean(r.Accessories);
        record.ExtraCharges = totalCharges;
        foreach (var path in r.ImagePaths)
            record.Images.Add(new HandoverImage { Path = path, Kind = HandoverImageKind.Return });

        // Ghi thời điểm trả trước để trigger trạng thái thấy xe đã được nhận lại. Vẫn cùng transaction.
        await db.SaveChangesAsync(ct);
        foreach (var line in contract.Lines)
            line.Vehicle.Status = checks.Single(x => x.VehicleId == line.VehicleId).NeedsMaintenance
                ? VehicleStatus.Maintenance : VehicleStatus.PendingInspection;

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = r.StaffId,
            ObjectType = "HopDong",
            ObjectId = contract.Code,
            Action = "NHAN_LAI_XE",
            OldValue = "trangThai=2",
            NewValue = $"phuPhi={totalCharges}; soXe={checks.Count}; daNhanDuXe=true",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return record;
    }

    /// <summary>FR-4.4: xe chỉ về Sẵn sàng khi giao nhận xác nhận kiểm tra đạt; không đạt thì chuyển Bảo trì.</summary>
    public async Task CompleteInspectionAsync(int vehicleId, int staffId, bool passed, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, ct)
            ?? throw new DomainException("Không tìm thấy xe.", 404);
        if (vehicle.Status != VehicleStatus.PendingInspection)
            throw new DomainException("Chỉ kiểm tra xe đang ở trạng thái Chờ kiểm tra.", 409);

        var now = VietnamTime.Now;
        vehicle.Status = passed ? VehicleStatus.Ready : VehicleStatus.Maintenance;
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = staffId,
            ObjectType = "Xe",
            ObjectId = vehicle.Id.ToString(),
            Action = passed ? "KIEM_TRA_DAT" : "KIEM_TRA_KHONG_DAT",
            OldValue = "trangThai=2",
            NewValue = $"trangThai={(int)vehicle.Status}",
            At = now
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// FR-5.1, BR-04: ghi nhận tiền thuê, phụ phí hoặc hoàn cọc. Khi thu đủ và hoàn đủ, hợp đồng chuyển sang Hoàn thành
    /// (UC-03 bước 7b). Đặt cọc ghi ở màn hình hợp đồng; giữ cọc khi hủy ghi trong luồng hủy.
    /// </summary>
    public async Task<Payment> RecordSettlementPaymentAsync(int contractId, PaymentType type, decimal amount,
        string? method, int actorId, CancellationToken ct = default)
    {
        if (type is PaymentType.Deposit or PaymentType.Forfeit)
            throw new DomainException("Loại giao dịch này không ghi nhận ở màn hình thanh toán sau thuê.");
        if (amount <= 0)
            throw new DomainException("Số tiền phải lớn hơn 0 (BR-02).");
        if (!PaymentMethods.IsAllowed(method))
            throw new DomainException("Hình thức thanh toán không hợp lệ.");

        var contract = await LoadContractAsync(contractId, ct);
        var returned = contract.Handover?.ReturnedAt is not null;
        var balance = SettlementRules.Balance(contract);

        if (type == PaymentType.Rent)
        {
            if (contract.Status != ContractStatus.Active)
                throw new DomainException("Chỉ thu tiền thuê khi hợp đồng đang thuê.", 409);
            if (amount > balance.OutstandingRent)
                throw new DomainException("Số tiền vượt quá số còn phải thu.", 409);
        }
        else if (type == PaymentType.Surcharge)
        {
            if (contract.Status != ContractStatus.Active || !returned)
                throw new DomainException("Phụ phí chỉ ghi nhận sau khi nhận lại xe (FR-4.3).", 409);
            if (amount > balance.OutstandingRent)
                throw new DomainException("Số tiền vượt quá số còn phải thu.", 409);
        }
        else
        {
            var refundable = contract.Status == ContractStatus.Cancelled
                             || (contract.Status == ContractStatus.Active && returned);
            if (!refundable)
                throw new DomainException("Chỉ hoàn cọc sau khi nhận lại xe hoặc khi hợp đồng đã hủy.", 409);
            if (amount > balance.OutstandingRefund)
                throw new DomainException("Số tiền hoàn vượt quá số cọc còn phải hoàn.", 409);
        }

        var now = VietnamTime.Now;
        await using var tx = await db.BeginTransactionAsync(ct);

        var payment = new Payment
        {
            Code = NewCode("GD", now),
            Amount = amount,
            Type = type,
            Method = string.IsNullOrWhiteSpace(method) ? "Tiền mặt" : method.Trim(),
            PaidAt = now,
            StatusCode = 1,
            ActorId = actorId
        };
        contract.Payments.Add(payment);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ObjectType = "ThanhToan",
            ObjectId = payment.Code,
            Action = "GHI_NHAN_SAU_THUE",
            NewValue = $"loai={(int)type}; soTien={amount}; hopDong={contract.Code}",
            At = now
        });

        if (contract.Status == ContractStatus.Active && returned && SettlementRules.Balance(contract).IsSettled)
        {
            contract.Status = ContractStatus.Completed;
            contract.History.Add(new ContractHistory
            {
                FromStatus = ContractStatus.Active,
                ToStatus = ContractStatus.Completed,
                ChangedAt = now,
                ChangedById = actorId,
                Note = "Thu đủ tiền thuê và hoàn đủ cọc (UC-03 bước 7b)"
            });
            db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                ObjectType = "HopDong",
                ObjectId = contract.Code,
                Action = "HOAN_THANH_HOP_DONG",
                OldValue = "trangThai=2",
                NewValue = "trangThai=3",
                At = now
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    private async Task<RentalContract> LoadContractAsync(int id, CancellationToken ct) =>
        await db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .Include(c => c.Handover).ThenInclude(h => h.Images)
            .Include(c => c.Payments)
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new DomainException("Không tìm thấy hợp đồng.", 404);

    private static void ValidateInspection(int km, int fuelPercent, string condition, IReadOnlyList<string> images)
    {
        if (km < 0) throw new DomainException("Số km không được âm (BR-02).");
        if (fuelPercent is < 0 or > 100) throw new DomainException("Mức nhiên liệu phải từ 0 đến 100%.");
        if (string.IsNullOrWhiteSpace(condition)) throw new DomainException("Vui lòng ghi tình trạng xe.");
        if (images.Count > MaxImages) throw new DomainException($"Mỗi biên bản tối đa {MaxImages} ảnh (A6).");
    }

    private static List<VehicleInspection> ResolveChecks(RentalContract contract, IReadOnlyList<VehicleInspection>? supplied,
        int km, int fuel, string condition, decimal damage = 0, bool maintenance = false)
    {
        var checks = supplied?.ToList() ?? [];
        if (checks.Count == 0 && contract.Lines.Count == 1)
            checks.Add(new(contract.Lines[0].VehicleId, km, fuel, condition, damage, maintenance));
        var expected = contract.Lines.Select(l => l.VehicleId).OrderBy(x => x).ToArray();
        if (expected.Length == 0 || !checks.Select(x => x.VehicleId).OrderBy(x => x).SequenceEqual(expected))
            throw new DomainException("Phải kiểm tra và ghi nhận đủ tất cả xe trong hợp đồng, mỗi xe đúng một lần.");
        foreach (var check in checks)
        {
            ValidateInspection(check.Km, check.FuelPercent, check.Condition, []);
            if (check.DamageCharge < 0) throw new DomainException("Chi phí hư hỏng không được âm.");
        }
        return checks;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NewCode(string prefix, DateTime now) =>
        $"{prefix}{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
