using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class DriverLicense
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string LicenseNumber { get; set; } = null!;

    public string LicenseClass { get; set; } = null!;

    public DateOnly ExpiryDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
