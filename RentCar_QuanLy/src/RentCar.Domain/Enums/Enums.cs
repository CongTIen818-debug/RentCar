namespace RentCar.Domain.Enums;

/// <summary>Trạng thái vận hành của xe (SRS mục 3.8, bảng 5.3a). Lịch đặt không nằm ở đây.</summary>
public enum VehicleStatus
{
    Ready = 1,
    PendingInspection = 2,
    Maintenance = 3,
    Retired = 4
}

/// <summary>Trạng thái yêu cầu thuê.</summary>
public enum RentalRequestStatus
{
    Pending = 1,
    ConvertedToContract = 2,
    Cancelled = 3
}

/// <summary>Trạng thái hợp đồng thuê. Quá hạn là cờ tính từ dữ liệu, không phải trạng thái (BR-13).</summary>
public enum ContractStatus
{
    PendingConfirmation = 0,
    Confirmed = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>Loại giao dịch tài chính (bảng 5.3a).</summary>
public enum PaymentType
{
    Deposit = 1,
    Rent = 2,
    Surcharge = 3,
    DepositRefund = 4,
    Forfeit = 5
}

/// <summary>Loại ảnh biên bản: 1 giao xe, 2 nhận lại xe (CHECK trong bảng anhBienBan).</summary>
public enum HandoverImageKind
{
    Delivery = 1,
    Return = 2
}

/// <summary>Hình thức thanh toán được CSDL chấp nhận (CK_thanhToan_phuongThuc).</summary>
public static class PaymentMethods
{
    public static readonly string[] All = ["Tiền mặt", "Chuyển khoản", "Cổng thử nghiệm"];

    public static bool IsAllowed(string? method) => method is null || All.Contains(method);
}

/// <summary>Mã vai trò, khớp cột maVaiTro trong bảng vaiTro.</summary>
public static class RoleCodes
{
    public const string Admin = "ADMIN";
    public const string Sales = "KINHDOANH";
    public const string Handover = "GIAONHAN";
    public const string Accounting = "KETOAN";
    public const string Customer = "KHACHHANG";
}
