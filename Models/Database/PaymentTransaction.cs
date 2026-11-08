using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class PaymentTransaction
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentType { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual RentalContract Contract { get; set; } = null!;

    public virtual AppUser CreatedByNavigation { get; set; } = null!;
}
