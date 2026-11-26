using Microsoft.EntityFrameworkCore;
using RentCar.Application.Rentals;
using RentCar.Domain.Enums;
using RentCar.Infrastructure.Persistence;
using RentCar.Models;

namespace RentCar.Services;

// Read-only adapter: reuse the member's entities, ownership query and deposit receipts; no state transitions.
public sealed class CustomerContracts(AppDbContext database, RentalService rentals)
{
    public async Task<CustomerContractsPage> ListAsync(int customerId, int page, ContractStatus? status, string? query, CancellationToken ct)
    {
        var owned = database.Contracts.AsNoTracking().Where(c => c.CustomerId == customerId);
        var counts = await owned.GroupBy(c => c.Status).Select(g => new CustomerContractCount(g.Key, g.Count())).ToListAsync(ct);
        if (status.HasValue) owned = owned.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(query)) owned = owned.Where(c => c.Code.Contains(query));
        var count = await owned.CountAsync(ct);
        var pages = Math.Max(1, (int)Math.Ceiling(count / 6d));
        page = Math.Clamp(page, 1, pages);
        var contracts = await owned.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((page - 1) * 6).Take(6)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .Include(c => c.PickupLocation).Include(c => c.ReturnLocation)
            .Include(c => c.Payments).Include(c => c.Handover).AsSplitQuery().ToListAsync(ct);
        return new(contracts.Select(c => new CustomerContractCard(c)).ToList(), counts, counts.Sum(c => c.Count), page, pages, status, query);
    }
    public async Task<CustomerContractDetails?> FindAsync(int customerId, int contractId, CancellationToken ct)
    {
        var contract = await rentals.GetContractAsync(contractId, customerId, ct);
        return contract is null ? null : new(contract);
    }
    public Task<IReadOnlyList<DepositReceiptLine>> ReceiptsAsync(int contractId, CancellationToken ct) => rentals.GetDepositReceiptsAsync(contractId, ct);
}
