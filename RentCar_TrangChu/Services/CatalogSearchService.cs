using Microsoft.EntityFrameworkCore;
using RentCar.Domain.Common;
using RentCar.Models;
namespace RentCar.Services;
public sealed class CatalogSearchService(VehicleCatalog catalog)
{
    public async Task<CatalogPage> SearchAsync(CatalogSearch search, CancellationToken ct)
    {
        search.Validate();
        if (search.PickupAt.HasValue != search.ReturnAt.HasValue) throw new DomainException("Vui lòng nhập đủ ngày giờ nhận và trả.");
        if (search.PickupAt is { } start && search.ReturnAt is { } end)
            CustomerRequestSubmission.ValidateSchedule(start, end, VietnamTime.Now);
        var query = catalog.Query(atVietnam: search.PickupAt is { } at ? at : null);
        if (search.PickupAt.HasValue)
            query = await catalog.AvailableAsync(query, search.PickupAt.Value, search.ReturnAt!.Value, ct);
        // Aggregate and paginate in SQL; do not load the entire fleet to render one page.
        var brands = await query.Where(c=>c.Brand!=null).GroupBy(c=>c.Brand!).OrderBy(g=>g.Key).Select(g=>new FilterOption<string>(g.Key,g.Count())).ToListAsync(ct);
        var types = await query.GroupBy(c=>c.VehicleType).OrderBy(g=>g.Key).Select(g=>new FilterOption<string>(g.Key,g.Count())).ToListAsync(ct);
        var seats = await query.GroupBy(c=>c.Seats).OrderBy(g=>g.Key).Select(g=>new FilterOption<int>(g.Key,g.Count())).ToListAsync(ct);
        var transmissions = await query.GroupBy(c=>c.Transmission).OrderBy(g=>g.Key).Select(g=>new FilterOption<string>(g.Key,g.Count())).ToListAsync(ct);
        var fuels = await query.GroupBy(c=>c.Fuel).OrderBy(g=>g.Key).Select(g=>new FilterOption<string>(g.Key,g.Count())).ToListAsync(ct);
        var highest = await query.Select(c=>(decimal?)c.DailyRate).MaxAsync(ct) ?? 0;
        if (search.Q is { } q) query=query.Where(c=>c.Name.Contains(q) || (c.Brand!=null && c.Brand.Contains(q)) || (c.Edition!=null && c.Edition.Contains(q)));
        if (search.Brands.Length>0) query=query.Where(c=>c.Brand!=null && search.Brands.Contains(c.Brand));
        if (search.Types.Length>0) query=query.Where(c=>search.Types.Contains(c.VehicleType));
        if (search.Seats.Length>0) query=query.Where(c=>search.Seats.Contains(c.Seats));
        if (search.Transmissions.Length>0) query=query.Where(c=>search.Transmissions.Contains(c.Transmission));
        if (search.Fuels.Length>0) query=query.Where(c=>search.Fuels.Contains(c.Fuel));
        if (search.MinPrice.HasValue) query=query.Where(c=>c.DailyRate>=search.MinPrice);
        if (search.MaxPrice.HasValue) query=query.Where(c=>c.DailyRate<=search.MaxPrice);
        var total=await query.CountAsync(ct); var pages=Math.Max(1,(int)Math.Ceiling(total/(double)search.Size)); var page=Math.Min(search.Page,pages);
        var ordered=search.Sort switch
        { "price-asc"=>query.OrderBy(c=>c.DailyRate).ThenBy(c=>c.Id), "price-desc"=>query.OrderByDescending(c=>c.DailyRate).ThenBy(c=>c.Id),
          "name"=>query.OrderBy(c=>c.Name).ThenBy(c=>c.Id), "seats"=>query.OrderBy(c=>c.Seats).ThenBy(c=>c.Id), _=>query.OrderBy(c=>c.Id) };
        return new(await ordered.Skip((page-1)*search.Size).Take(search.Size).ToListAsync(ct),total,page,pages,highest,brands,types,seats,transmissions,fuels);
    }
}
