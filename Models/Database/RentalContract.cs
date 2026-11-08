using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class RentalContract
{
    public int Id { get; set; }

    public string ContractCode { get; set; } = null!;

    public int RentalRequestId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime? ActualReturnAt { get; set; }

    public decimal RentalPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal DepositAmount { get; set; }

    public int KmLimit { get; set; }

    public string Status { get; set; } = null!;

    public string? CancelReason { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual ICollection<ContractHistory> ContractHistories { get; set; } = new List<ContractHistory>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<HandoverRecord> HandoverRecords { get; set; } = new List<HandoverRecord>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual RentalRequest RentalRequest { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
