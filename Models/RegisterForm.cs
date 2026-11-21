using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;

public class RegisterForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    [RegularExpression(@"^[!-~]+$", ErrorMessage = "Email chỉ dùng ký tự không dấu và không chứa khoảng trắng.")]
    public string Email { get; set; } = "";

    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [RegularExpression(@"^\+?[0-9][0-9 .-]{7,19}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(256, MinimumLength = 6, ErrorMessage = "Mật khẩu cần từ 6 đến 256 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
