using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class PriceRule
{
    public int Id { get; set; }

    public string VehicleType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Multiplier { get; set; }
}
