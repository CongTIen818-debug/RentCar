using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class SurchargePolicy
{
    public int Id { get; set; }

    public string VehicleType { get; set; } = null!;

    public int GraceMinutes { get; set; }

    public int KmLimitPerDay { get; set; }

    public decimal ExtraKmRate { get; set; }
}
