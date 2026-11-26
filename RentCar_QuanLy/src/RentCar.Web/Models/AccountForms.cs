using System.ComponentModel.DataAnnotations;
using RentCar.Domain.Enums;

namespace RentCar.Web.Models;

public sealed class ChangePasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ForgotPasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";
}

public sealed class ResetPasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mã đặt lại.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ProfileForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    public string Phone { get; set; } = "";

    [StringLength(255)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số GPLX.")]
    [StringLength(30)]
    public string LicenseNumber { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn hạng GPLX.")]
    public string LicenseClass { get; set; } = "B";

    [Required(ErrorMessage = "Vui lòng nhập ngày hết hạn GPLX.")]
    public DateOnly? LicenseExpiry { get; set; }
}

public sealed class StaffInvitationForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email của nhân viên.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string RoleCode { get; set; } = RoleCodes.Sales;
}

public sealed class StaffRegisterForm
{
    [Required(ErrorMessage = "Vui lòng nhập mã đăng ký.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email được cấp mã.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class StaffEditForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    public string FullName { get; set; } = "";

    public string? Email { get; set; }

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string RoleCode { get; set; } = RoleCodes.Sales;
}
