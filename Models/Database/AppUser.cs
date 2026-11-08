using System;
using System.Collections.Generic;

namespace RentCar.Models.Database;

public partial class AppUser
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public string? ResetTokenHash { get; set; }

    public DateTime? ResetTokenExpiresAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<ContractHistory> ContractHistories { get; set; } = new List<ContractHistory>();

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<HandoverRecord> HandoverRecords { get; set; } = new List<HandoverRecord>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
}
