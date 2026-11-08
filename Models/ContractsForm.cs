using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;
public class ContractsForm
{
    [Display(Name = "ID yêu cầu thuê")]
    [Range(1, int.MaxValue)]
    public int RentalRequestId { get; set; }

    [Display(Name = "Mã hợp đồng")]
    [Required, StringLength(40)]
    public string ContractCode { get; set; } = "";

    [Display(Name = "Giá thuê")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal RentalPrice { get; set; }

    [Display(Name = "Giảm giá")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Tiền cọc")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Giới hạn km")]
    [Range(0, int.MaxValue)]
    public int KmLimit { get; set; }
}
