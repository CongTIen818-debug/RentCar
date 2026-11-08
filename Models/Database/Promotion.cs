using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class Promotion
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountPercent { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }
}
