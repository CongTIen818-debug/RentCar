using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ContractId { get; set; }

    public string Type { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public string Mode { get; set; } = null!;

    public virtual RentalContract? Contract { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
