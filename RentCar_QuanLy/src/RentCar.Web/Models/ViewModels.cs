using RentCar.Application.Rentals;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentCar.Application.Catalog;
using RentCar.Domain.Entities;
using RentCar.Domain.Rules;

namespace RentCar.Web.Models;

public sealed class CatalogSearchForm
{
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int? VehicleTypeId { get; set; }
    public string? Keyword { get; set; }
    public int? MinSeats { get; set; }
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public decimal? MaxDailyRate { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CatalogIndexVm
{
    public CatalogSearchForm Form { get; set; } = new();
    public CatalogPage? Page { get; set; }
    public List<SelectListItem> Types { get; set; } = new();
    public string? Error { get; set; }
}

public sealed class VehicleDetailVm
{
    public VehicleDetail Detail { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<PickupLocation> Locations { get; set; } = new();
    /// <summary>Email hoặc số điện thoại khách mà nhân viên đã nhập khi đặt hộ; giữ lại để không phải gõ lại sau khi có lỗi.</summary>
    public string? CustomerKey { get; set; }
}

public sealed class MyRentalsVm
{
    public List<RentalRequest> Requests { get; set; } = new();
    public List<RentalContract> Contracts { get; set; } = new();
}

public sealed class StaffIndexVm
{
    public List<RentalRequest> Requests { get; set; } = new();
    public List<RentalContract> Contracts { get; set; } = new();
}

public sealed class ContractVm
{
    public RentalContract Contract { get; set; } = null!;
    public bool IsStaff { get; set; }
    public IReadOnlyList<ConfirmationCheck> Checks { get; set; } = [];
    public bool CanConfirm { get; set; }
    public decimal Paid { get; set; }
    public ContractBalance Balance { get; set; } = new(0, 0, 0, 0);
    /// <summary>UC-03 A4: đã quá 4 giờ kể từ giờ nhận xe mà chưa bàn giao.</summary>
    public bool NoShowAvailable { get; set; }
}

/// <summary>v8 (#13): dữ liệu cho trang biên nhận đặt cọc.</summary>
public sealed class DepositReceiptVm
{
    public RentalContract Contract { get; set; } = null!;
    public bool IsStaff { get; set; }
    public IReadOnlyList<DepositReceiptLine> Lines { get; set; } = [];
    public decimal Total => Lines.Sum(l => l.Amount);
}
