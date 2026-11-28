using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RentCar.Domain.Common;
using RentCar.Models;
using RentCar.Services;
namespace RentCar.Controllers;
public class HomeController(CatalogSearchService searcher, VehicleCatalog catalog, RentalSelection selection, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index([FromQuery] CatalogSearch search, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid) throw new DomainException("Thông tin tìm kiếm không hợp lệ.");
            var locations=await catalog.LocationsAsync(cancellationToken);
            if (!Request.Query.ContainsKey("locationId")) search.LocationId=selection.LocationId();
            if (search.LocationId.HasValue && !locations.Any(l=>l.Id==search.LocationId)) throw new DomainException("Điểm giao nhận không hợp lệ. Vui lòng chọn lại.");
            var isSearch=search.PickupAt.HasValue || search.ReturnAt.HasValue;
            if (isSearch && search.LocationId is null) throw new DomainException("Vui lòng chọn điểm nhận và trả xe.");
            var result=await searcher.SearchAsync(search,cancellationToken);
            if(search.LocationId.HasValue) selection.SetLocation(search.LocationId.Value);
            return View(new VehicleFilterViewModel(result.Brands,search.Brands,result.Seats,search.Seats,result.Types,search.Types,
                result.HighestPrice,search.MaxPrice??result.HighestPrice,result.Total,search.PickupAt,search.ReturnAt,isSearch,null,result.Cars)
                {Locations=locations,LocationId=search.LocationId,Search=search,Page=result.Page,Pages=result.Pages,Transmissions=result.Transmissions,Fuels=result.Fuels});
        }
        catch(DomainException ex)
        {
            Response.StatusCode=400;
            return View(new VehicleFilterViewModel([],search.Brands,[],search.Seats,[],search.Types,0,0,0,search.PickupAt,search.ReturnAt,true,ex.Message,[]) {Search=search,Locations=[],LocationId=search.LocationId});
        }
        catch(SqlException ex) {logger.LogError(ex,"Unable to search fleet.");Response.StatusCode=503;return View();}
    }
    public IActionResult Privacy()=>View();
    [ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public IActionResult Error()=>View(new ErrorViewModel {RequestId=Activity.Current?.Id??HttpContext.TraceIdentifier});
}
