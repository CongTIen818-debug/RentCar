using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string IdentityNumber { get; set; } = null!;

    public string? Address { get; set; }

    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public virtual DriverLicense? DriverLicense { get; set; }

    public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

    public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
}
