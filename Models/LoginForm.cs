using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(256, ErrorMessage = "Mật khẩu tối đa 256 ký tự.")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
