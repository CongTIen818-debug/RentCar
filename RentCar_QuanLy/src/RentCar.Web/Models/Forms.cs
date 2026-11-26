using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RentCar.Domain.Enums;
using RentCar.Domain.Pricing;

namespace RentCar.Web.Models;

public sealed class LoginForm
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số giấy tờ tùy thân (giả lập).")]
    public string IdNumber { get; set; } = "";

    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số GPLX.")]
    public string LicenseNumber { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn hạng GPLX.")]
    public string LicenseClass { get; set; } = "B";

    [Required(ErrorMessage = "Vui lòng nhập ngày hết hạn GPLX.")]
    public DateOnly? LicenseExpiry { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class RentalRequestForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Xe không hợp lệ.")]
    public int VehicleId { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PickupLocationId { get; set; }
    public int ReturnLocationId { get; set; }
}

/// <summary>Đặt cọc ghi tại hợp đồng chờ xác nhận (FR-3.3b).</summary>
public sealed class PaymentForm
{
    public int ContractId { get; set; }
    public PaymentType Type { get; set; } = PaymentType.Deposit;
    public decimal Amount { get; set; }
    public string? Method { get; set; } = "Tiền mặt";
}

/// <summary>UC-03 bước 3: biên bản bàn giao.</summary>
public sealed class DeliveryForm
{
    public int ContractId { get; set; }
    public DateTime? DeliveredAt { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Số km không hợp lệ.")]
    public int Km { get; set; }

    [Range(0, 100, ErrorMessage = "Nhiên liệu phải từ 0 đến 100%.")]
    public int FuelPercent { get; set; } = 100;

    [Required(ErrorMessage = "Vui lòng ghi tình trạng xe.")]
    public string Condition { get; set; } = "Đạt, không trầy xước";

    public string? Accessories { get; set; } = "Giấy tờ xe, chìa khóa, khăn lau";
    public bool IdentityAndVehicleChecked { get; set; }
    public List<IFormFile> Images { get; set; } = new();
    public List<RentCar.Application.Handover.VehicleInspection> Vehicles { get; set; } = new();
}

/// <summary>UC-03 bước 5 đến 7a: biên bản nhận lại xe.</summary>
public sealed class ReturnForm
{
    public int ContractId { get; set; }
    public DateTime? ReturnedAt { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Số km không hợp lệ.")]
    public int Km { get; set; }

    [Range(0, 100, ErrorMessage = "Nhiên liệu phải từ 0 đến 100%.")]
    public int FuelPercent { get; set; } = 100;

    [Required(ErrorMessage = "Vui lòng ghi tình trạng xe.")]
    public string Condition { get; set; } = "Đạt, không trầy xước";

    public string? Accessories { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Chi phí hư hỏng không được âm.")]
    public decimal DamageCharge { get; set; }

    public bool NeedsMaintenance { get; set; }
    public List<IFormFile> Images { get; set; } = new();
    public List<RentCar.Application.Handover.VehicleInspection> Vehicles { get; set; } = new();
}

public sealed class CancelForm
{
    public int ContractId { get; set; }
    public CancellationKind Kind { get; set; } = CancellationKind.CustomerCancel;

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy.")]
    public string Reason { get; set; } = "";
}

/// <summary>FR-4.4: kết quả kiểm tra xe sau khi trả.</summary>
public sealed class InspectionForm
{
    public int VehicleId { get; set; }
    public bool Passed { get; set; }
}
