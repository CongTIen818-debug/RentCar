using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class HandoverImage
{
    public int Id { get; set; }

    public int HandoverId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public virtual HandoverRecord Handover { get; set; } = null!;
}
