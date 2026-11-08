using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class MaintenanceRecord
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public string MaintenanceType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public decimal Cost { get; set; }

    public string Status { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
