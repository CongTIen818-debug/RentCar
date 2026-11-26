using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RentCar.Web.Models;

public sealed class VehicleForm
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(20)] public string PlateNumber { get; set; } = "";
    [Required, StringLength(50)] public string Brand { get; set; } = "";
    [StringLength(100)] public string? Model { get; set; }
    [Range(1886, 2100)] public int ModelYear { get; set; } = DateTime.Now.Year;
    [StringLength(30)] public string? Color { get; set; }
    [Range(1, 255)] public int Seats { get; set; } = 5;
    [Required] public string Transmission { get; set; } = "Tự động";
    [Required] public string FuelType { get; set; } = "Xăng";
    [Range(typeof(decimal), "0", "1000000000")] public decimal DailyRate { get; set; }
    [Range(typeof(decimal), "0", "1000000000")] public decimal WeeklyRate { get; set; }
    [Range(typeof(decimal), "0", "1000000000")] public decimal DepositAmount { get; set; }
    [Range(1, int.MaxValue)] public int VehicleTypeId { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    public IFormFile? Image { get; set; }
}

public sealed class VehicleStatusForm
{
    public int Id { get; set; }
    public int Status { get; set; }
}
