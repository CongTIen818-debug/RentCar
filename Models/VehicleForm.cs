using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;

public class VehicleForm
{
    [Display(Name = "Biển số")]
    [Required, StringLength(20)]
    public string PlateNumber { get; set; } = "";

    [Display(Name = "Hãng xe")]
    [Required, StringLength(80)]
    public string Brand { get; set; } = "";

    [Display(Name = "Dòng xe")]
    [Required, StringLength(80)]
    public string Model { get; set; } = "";

    [Display(Name = "Loại xe")]
    [Required, StringLength(50)]
    public string VehicleType { get; set; } = "";

    [Display(Name = "Số chỗ")]
    [Range(1, 255)]
    public byte Seats { get; set; }

    [Display(Name = "Hộp số")]
    [Required, StringLength(30)]
    public string Transmission { get; set; } = "";

    [Display(Name = "Nhiên liệu")]
    [Required, StringLength(30)]
    public string FuelType { get; set; } = "";

    [Display(Name = "Năm sản xuất")]
    [Range(1886, 2100)]
    public short ManufactureYear { get; set; }

    [Display(Name = "Giá ngày (₫)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DailyRate { get; set; }

    [Display(Name = "Giá tuần (₫)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal WeeklyRate { get; set; }

    [Display(Name = "Tiền cọc (₫)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DepositAmount { get; set; }
}
