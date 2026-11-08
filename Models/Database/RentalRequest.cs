using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class RentalRequest
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int PickupLocationId { get; set; }

    public int ReturnLocationId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual PickupLocation PickupLocation { get; set; } = null!;

    public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

    public virtual PickupLocation ReturnLocation { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
