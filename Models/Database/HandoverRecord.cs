using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class HandoverRecord
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public string RecordType { get; set; } = null!;

    public DateTime RecordedAt { get; set; }

    public int RecordedBy { get; set; }

    public int Odometer { get; set; }

    public byte FuelLevel { get; set; }

    public string? Accessories { get; set; }

    public string? ConditionNote { get; set; }

    public virtual RentalContract Contract { get; set; } = null!;

    public virtual ICollection<HandoverImage> HandoverImages { get; set; } = new List<HandoverImage>();

    public virtual AppUser RecordedByNavigation { get; set; } = null!;
}
