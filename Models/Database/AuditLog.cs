using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class AuditLog
{
    public int Id { get; set; }

    public string ObjectType { get; set; } = null!;

    public int ObjectId { get; set; }

    public string Action { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
