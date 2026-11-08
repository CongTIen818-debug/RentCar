using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class VehicleImage
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
