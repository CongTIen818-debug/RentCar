using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class PickupLocation
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<RentalRequest> RentalRequestPickupLocations { get; set; } = new List<RentalRequest>();

    public virtual ICollection<RentalRequest> RentalRequestReturnLocations { get; set; } = new List<RentalRequest>();
}
