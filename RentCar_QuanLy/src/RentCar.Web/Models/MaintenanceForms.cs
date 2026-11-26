using System.ComponentModel.DataAnnotations;

namespace RentCar.Web.Models;

public sealed class MaintenanceForm
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập loại công việc."), StringLength(50)] public string Type { get; set; } = "Bảo dưỡng định kỳ";
    [StringLength(255)] public string? Description { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")] public DateTime? StartAt { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")] public DateTime? EndAt { get; set; }
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Chi phí phải từ 0 trở lên.")] public decimal Cost { get; set; }
}
