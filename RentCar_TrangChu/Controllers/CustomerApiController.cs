using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Auth;
using RentCar.Application.Rentals;
using RentCar.Domain.Common;
using RentCar.Models;
using RentCar.Services;
using RentCar.Web.Models;
namespace RentCar.Controllers;
public sealed class RequestDraft
{
    [MinLength(1),MaxLength(100)] public int[] VehicleIds {get;set;}=[];
    public int[] DriverIds {get;set;}=[];
    [Required] public DateTime? PickupAt {get;set;}
    [Required] public DateTime? ReturnAt {get;set;}
    [Range(1,int.MaxValue)] public int LocationId {get;set;}
}
[ApiController]
[Route("api")]
[RequestSizeLimit(16384)]
public sealed class CustomerApiController(CatalogSearchService searcher,VehicleCatalog catalog,RentalPricing pricing,
    AuthService auth,CustomerRequestSubmission submission,CustomerRequests requests,IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf"),AllowAnonymous]
    public IActionResult Csrf(){Response.Headers.CacheControl="no-store";return Ok(new {token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken,header="X-CSRF-TOKEN"});}
    [HttpGet("vehicles"),AllowAnonymous]
    public async Task<IActionResult> Vehicles([FromQuery] CatalogSearch search,CancellationToken ct)
    {
        if(!Request.Query.ContainsKey("size")) search.Size=20;
        var page=await searcher.SearchAsync(search,ct);
        return Ok(new {items=page.Cars,page=page.Page,size=search.Size,total=page.Total,pages=page.Pages});
    }
    [HttpGet("vehicles/{id:int}"),AllowAnonymous]
    public async Task<IActionResult> Vehicle(int id,CancellationToken ct)
    {
        var car=await catalog.Query(false).SingleOrDefaultAsync(c=>c.Id==id,ct);
        return car is null?NotFound(new {code="NOT_FOUND",message="Không tìm thấy xe."}):Ok(car);
    }
    [HttpGet("pickup-locations"),AllowAnonymous]
    public async Task<IActionResult> Locations(CancellationToken ct)=>Ok(await catalog.LocationsAsync(ct));
    [HttpPost("auth/register"),AllowAnonymous,ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterForm form,CancellationToken ct)
    {
        await auth.RegisterCustomerAsync(new RegisterCustomerRequest(form.FullName,form.BirthDate,form.Phone,form.Email,form.IdNumber,form.Address,form.LicenseNumber,form.LicenseClass,form.LicenseExpiry,form.Password,form.ConfirmPassword),ct);
        return StatusCode(201,new {message="Đăng ký thành công. Vui lòng đăng nhập."});
    }
    [HttpPost("auth/login"),AllowAnonymous,ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form,CancellationToken ct)
    {
        try
        {
            var user=await auth.SignInAsync(form.Email,form.Password,ct);
            if(user.Role.Code!="KHACHHANG")return StatusCode(403,new {code="FORBIDDEN",message="API này dành cho tài khoản khách hàng."});
            var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.FullName),new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,user.Role.Code)},CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity));
            return Ok(new {id=user.Id,name=user.FullName});
        }
        catch(DomainException ex){return StatusCode(ex.StatusCode==401?401:403,new {code="AUTH_FAILED",message="Không thể đăng nhập. Vui lòng kiểm tra thông tin hoặc thử lại sau."});}
    }
    [HttpPost("auth/logout"),Authorize(Roles="KHACHHANG"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return Ok(new {message="Đã đăng xuất."});}
    [HttpGet("rental-requests"),Authorize(Roles="KHACHHANG")]
    public async Task<IActionResult> Requests(int page=1,int? status=null,CancellationToken ct=default)
    {
        if(page<1 || status.HasValue && status is not(1 or 2 or 3))throw new DomainException("Bộ lọc yêu cầu không hợp lệ.");
        return Ok(await requests.ListAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),page,status,ct));
    }
    [HttpPost("rental-requests"),Authorize(Roles="KHACHHANG"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestDraft draft,CancellationToken ct)
    {
        if(draft.DriverIds.Except(draft.VehicleIds).Any())throw new DomainException("Lựa chọn tài xế không thuộc danh sách xe.");
        var id=await submission.SubmitAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),draft.VehicleIds.Select(id=>new RequestedVehicle(id,draft.DriverIds.Contains(id))).ToArray(),draft.PickupAt!.Value,draft.ReturnAt!.Value,draft.LocationId,ct);
        return StatusCode(201,new {id,message="Đã gửi yêu cầu. Giữ chỗ 24 giờ kể từ lúc gửi.",url="/yeu-cau-cua-toi#yeu-cau-"+id});
    }
    [HttpPost("rental-quote"),AllowAnonymous,ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(RequestDraft draft,CancellationToken ct)
    {
        CustomerRequestSubmission.ValidateSchedule(draft.PickupAt!.Value,draft.ReturnAt!.Value,VietnamTime.Now);
        if(draft.DriverIds.Except(draft.VehicleIds).Any() || draft.VehicleIds.Distinct().Count()!=draft.VehicleIds.Length)throw new DomainException("Danh sách xe hoặc tài xế không hợp lệ.");
        var query=await catalog.AvailableAsync(catalog.Query(atVietnam:draft.PickupAt.Value),draft.PickupAt.Value,draft.ReturnAt.Value,ct);
        var cars=await query.Where(c=>draft.VehicleIds.Contains(c.Id)).ToListAsync(ct);
        if(cars.Count!=draft.VehicleIds.Length)throw new DomainException("Có xe không còn trống lịch.",409);
        if(cars.Any(c=>draft.DriverIds.Contains(c.Id)&&c.Seats<=2))throw new DomainException("Xe từ 2 chỗ trở xuống chỉ hỗ trợ tự lái.");
        var days=(int)Math.Ceiling((draft.ReturnAt.Value-draft.PickupAt.Value).TotalDays);
        return Ok(new {days,price=await pricing.QuoteAsync(cars,draft.DriverIds.Distinct().Count(),days,draft.PickupAt.Value,ct),taxIncluded=true});
    }
}
