using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class Vehicle
{
    public int Id { get; set; }

    public string PlateNumber { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string VehicleType { get; set; } = null!;

    public byte Seats { get; set; }

    public string Transmission { get; set; } = null!;

    public string FuelType { get; set; } = null!;

    public short ManufactureYear { get; set; }

    public decimal DailyRate { get; set; }

    public decimal WeeklyRate { get; set; }

    public decimal DepositAmount { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

    public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();

    public virtual ICollection<VehicleImage> VehicleImages { get; set; } = new List<VehicleImage>();
}
