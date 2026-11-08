using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;
public class EmployeesForm
{
    [Display(Name = "Họ tên")]
    [Required, StringLength(150)]
    public string FullName { get; set; } = "";

    [Display(Name = "Email")]
    [Required, StringLength(254)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "Vai trò")]
    [Required, StringLength(40)]
    public string Role { get; set; } = "";
}
