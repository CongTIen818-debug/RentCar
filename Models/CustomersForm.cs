using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;
public class CustomersForm
{
    [Display(Name = "Họ tên")]
    [Required, StringLength(150)]
    public string FullName { get; set; } = "";

    [Display(Name = "Ngày sinh")]
    [DataType(DataType.Date)]
    public DateOnly DateOfBirth { get; set; }

    [Display(Name = "Điện thoại")]
    [Required, StringLength(25)]
    public string Phone { get; set; } = "";

    [Display(Name = "Email")]
    [Required, StringLength(254)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "Giấy tờ giả lập")]
    [Required, StringLength(30)]
    public string IdentityNumber { get; set; } = "";

    [Display(Name = "Số GPLX giả lập")]
    [Required, StringLength(30)]
    public string LicenseNumber { get; set; } = "";

    [Display(Name = "Hạng GPLX")]
    [Required, StringLength(10)]
    public string LicenseClass { get; set; } = "";

    [Display(Name = "Hạn GPLX")]
    [DataType(DataType.Date)]
    public DateOnly ExpiryDate { get; set; }
}
