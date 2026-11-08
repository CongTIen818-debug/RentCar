using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class ContractHistory
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public int ChangedBy { get; set; }

    public string? Note { get; set; }

    public virtual AppUser ChangedByNavigation { get; set; } = null!;

    public virtual RentalContract Contract { get; set; } = null!;
}
