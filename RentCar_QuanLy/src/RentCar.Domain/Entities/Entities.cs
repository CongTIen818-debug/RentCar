using RentCar.Domain.Enums;

namespace RentCar.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    /// <summary>false = bị quản trị viên khóa (FR-6.4).</summary>
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    /// <summary>Khóa tạm 15 phút sau 5 lần sai liên tiếp (FR-6.4).</summary>
    public DateTime? LockedUntil { get; set; }
    /// <summary>SHA-256 của mã đặt lại mật khẩu (UC-07). NULL khi không có yêu cầu đang chờ; không lưu mã thô.</summary>
    public string? ResetCodeHash { get; set; }
    /// <summary>Mã đặt lại hết hạn sau 30 phút và chỉ dùng một lần (FR-6.1c).</summary>
    public DateTime? ResetCodeExpiresAt { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DriverLicense? DriverLicense { get; set; }
}

public class DriverLicense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string LicenseClass { get; set; } = "";
    public DateOnly ExpiryDate { get; set; }
    public User User { get; set; } = null!;
}

public class VehicleType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>Hạng GPLX tối thiểu (BR-03c).</summary>
    public string MinLicenseClass { get; set; } = "B";
}

public class Vehicle
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PlateNumber { get; set; } = "";
    public string Brand { get; set; } = "";
    public string? Model { get; set; }
    public int ModelYear { get; set; }
    public string? Color { get; set; }
    public int Seats { get; set; }
    public string Transmission { get; set; } = "";
    public string FuelType { get; set; } = "";
    public decimal DailyRate { get; set; }
    public decimal WeeklyRate { get; set; }
    public decimal DepositAmount { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Ready;
    public string? Description { get; set; }
    public string? PrimaryImagePath { get; set; }
    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;
    public List<VehicleImage> Images { get; set; } = new();
}

public class VehicleImage
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
}

public class PickupLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>Bảng giá theo loại xe và khoảng ngày: hệ số nhân giá ngày và giá tuần (BR-10a).</summary>
public class PriceRule
{
    public int Id { get; set; }
    public int VehicleTypeId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal Factor { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
}

/// <summary>Ưu đãi giảm theo phần trăm, áp dụng tối đa một ưu đãi cho mỗi hợp đồng (BR-10c).</summary>
public class Promotion
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PromotionVehicle> Vehicles { get; set; } = new();
}

public class PromotionVehicle
{
    public int PromotionId { get; set; }
    public int VehicleId { get; set; }
}

public class SurchargePolicy
{
    public int Id { get; set; }
    public int VehicleTypeId { get; set; }
    public int GraceMinutes { get; set; } = 120;
    public int DailyKmLimit { get; set; } = 200;
    public decimal PricePerExtraKm { get; set; } = 5000m;
}

public class RentalRequest
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? CreatedById { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PickupLocationId { get; set; }
    public int ReturnLocationId { get; set; }
    public RentalRequestStatus Status { get; set; } = RentalRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    /// <summary>Giữ chỗ 24 giờ (BR-15).</summary>
    public DateTime HoldUntil { get; set; }
    public User Customer { get; set; } = null!;
    public List<RentalRequestLine> Lines { get; set; } = new();
    public PickupLocation PickupLocation { get; set; } = null!;
    public PickupLocation ReturnLocation { get; set; } = null!;
}

public class RentalRequestLine
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int VehicleId { get; set; }
    public bool WithDriver { get; set; }
    public decimal DriverDailyFee { get; set; }
    public RentalRequest Request { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
}

public class RentalContract
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal GrossAmount { get; set; }
    public int? PromotionId { get; set; }
    public string? PromotionName { get; set; }
    public decimal? PromotionPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>Sao chép từ xe tại thời điểm lập hợp đồng (BR-14).</summary>
    public decimal DepositAmount { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.PendingConfirmation;
    public string? CancelReason { get; set; }
    public DateTime? HoldUntil { get; set; }
    public int CustomerId { get; set; }
    public int? CreatedById { get; set; }
    public int? RentalRequestId { get; set; }
    public int PickupLocationId { get; set; }
    public int ReturnLocationId { get; set; }

    /// <summary>Cột tongTienThanhToan trong CSDL là cột tính toán; ứng dụng chỉ đọc.</summary>
    public decimal DriverAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount => decimal.Round((GrossAmount - DiscountAmount + DriverAmount) * VatRate, 0, MidpointRounding.AwayFromZero);
    public decimal TotalPayable => GrossAmount - DiscountAmount + DriverAmount + VatAmount;

    public User Customer { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public PickupLocation PickupLocation { get; set; } = null!;
    public PickupLocation ReturnLocation { get; set; } = null!;
    public List<ContractLine> Lines { get; set; } = new();
    public HandoverRecord? Handover { get; set; }
    public List<ContractHistory> History { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

/// <summary>Một xe duy nhất trong hợp đồng nhiều xe, kèm giá và phí tài xế đã chốt.</summary>
public class ContractLine
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int VehicleId { get; set; }
    public decimal DailyRateAtBooking { get; set; }
    public decimal WeeklyRateAtBooking { get; set; }
    public int DayCount { get; set; }
    public int WeekCount { get; set; }
    public bool WithDriver { get; set; }
    public decimal DriverDailyFee { get; set; }

    /// <summary>Thành tiền gốc theo công thức của cột tính toán thanhTienGoc.</summary>
    public decimal GrossLine => WeekCount * WeeklyRateAtBooking + (DayCount - 7 * WeekCount) * DailyRateAtBooking;

    public RentalContract Contract { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
}

/// <summary>Lịch sử chuyển trạng thái hợp đồng (BR-08). Chỉ thêm, không sửa, không xóa.</summary>
public class ContractHistory
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public ContractStatus? FromStatus { get; set; }
    public ContractStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public int ChangedById { get; set; }
    public string? Note { get; set; }
    public RentalContract Contract { get; set; } = null!;
}

public class Payment
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public string? Method { get; set; }
    public DateTime PaidAt { get; set; }
    /// <summary>1 = hợp lệ; 0 = đã đảo bút toán (không xóa giao dịch, BR-05).</summary>
    public int StatusCode { get; set; } = 1;
    public int ActorId { get; set; }
    public RentalContract Contract { get; set; } = null!;
}

/// <summary>Nhật ký kiểm toán (NFR-09). BIGINT trong CSDL, chỉ thêm.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public int? ActorId { get; set; }
    public string ObjectType { get; set; } = "";
    public string? ObjectId { get; set; }
    public string Action { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime At { get; set; }
}

/// <summary>
/// Biên bản bàn giao và nhận lại xe (bảng bienBanBanGiao). Mỗi hợp đồng có đúng một dòng,
/// ghi cả thông tin lúc giao (FR-4.1) và lúc nhận lại (FR-4.2).
/// </summary>
public class HandoverRecord
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryChecksJson { get; set; }
    public string? ReturnChecksJson { get; set; }
    public int? DeliveryKm { get; set; }
    public int? DeliveryFuelPercent { get; set; }
    public string? DeliveryCondition { get; set; }
    public string? DeliveryAccessories { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public int? ReturnKm { get; set; }
    public int? ReturnFuelPercent { get; set; }
    public string? ReturnCondition { get; set; }
    public string? ReturnAccessories { get; set; }
    /// <summary>Tổng khoản phát sinh ghi tại biên bản: SP-01 + SP-02 + SP-03 (cột chiPhiPhatSinh).</summary>
    public decimal ExtraCharges { get; set; }
    public RentalContract Contract { get; set; } = null!;
    public List<HandoverImage> Images { get; set; } = new();
}

/// <summary>Ảnh đính kèm biên bản (bảng anhBienBan, FR-4.1, FR-4.2).</summary>
public class HandoverImage
{
    public int Id { get; set; }
    public int HandoverId { get; set; }
    public string Path { get; set; } = "";
    public HandoverImageKind Kind { get; set; }
    public HandoverRecord Handover { get; set; } = null!;
}

/// <summary>
/// Mã đăng ký nhân viên do quản trị viên cấp (bảng maDangKyNhanVien, v4). Chỉ lưu bản băm SHA-256 của mã;
/// mã thô chỉ hiện một lần lúc cấp. Mã dùng một lần, gắn với một email và một vai trò, hết hạn sau 7 ngày.
/// </summary>
public class StaffInvitation
{
    public int Id { get; set; }
    public string CodeHash { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    /// <summary>Thời điểm nhân viên đã dùng mã để tạo tài khoản; NULL nếu chưa dùng.</summary>
    public DateTime? UsedAt { get; set; }
    /// <summary>true = quản trị viên đã thu hồi, hoặc hệ thống tự thu hồi khi cấp mã mới cho mã đã hết hạn.</summary>
    public bool IsRevoked { get; set; }
    public DateTime IssuedAt { get; set; }
    public int IssuedById { get; set; }
    public User IssuedBy { get; set; } = null!;
}

/// <summary>Lịch bảo dưỡng, kiểm định của xe (bảng lichBaoTri, FR-1.4, BR-11).</summary>
public class MaintenanceSchedule
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal Cost { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
}
