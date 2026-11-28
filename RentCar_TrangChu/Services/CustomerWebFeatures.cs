using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RentCar.Domain.Common;
using RentCar.Infrastructure.Persistence;
namespace RentCar.Services;
public sealed class CustomerApiErrors(ILogger<CustomerApiErrors> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if(!context.HttpContext.Request.Path.StartsWithSegments("/api"))return;
        var ex=context.Exception;var duplicate=ex is DbUpdateException {InnerException:SqlException {Number:2601 or 2627}};
        var status=duplicate?409:ex is FieldValidationException?400:ex is DomainException d?d.StatusCode:ex is SqlException?503:500;
        if(status>=500)logger.LogError(ex,"Customer API request failed.");
        context.Result=new ObjectResult(new {code=status==409?"CONFLICT":status>=500?"UNAVAILABLE":"INVALID_REQUEST",message=duplicate?"Thông tin đã được sử dụng. Vui lòng kiểm tra và thử lại.":ex is FieldValidationException?"Dữ liệu chưa hợp lệ.":ex is DomainException domain?domain.Message:"Hệ thống chưa xử lý được. Vui lòng thử lại sau.",errors=ex is FieldValidationException f?f.Errors:null}){StatusCode=status};context.ExceptionHandled=true;
    }
}
public static class CustomerWebFeatures
{
    public static void AddCustomerFeatures(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddScoped<CatalogSearchService>();services.AddScoped<CustomerRecovery>();services.AddScoped<CustomerPrivacy>();services.AddScoped<CustomerNotifications>();
        services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");
        services.AddControllersWithViews(o=>o.Filters.Add<CustomerApiErrors>());
        services.Configure<ApiBehaviorOptions>(o=>o.InvalidModelStateResponseFactory=c=>new BadRequestObjectResult(new {code="INVALID_REQUEST",message="Dữ liệu gửi không hợp lệ.",errors=c.ModelState.Where(p=>p.Value?.Errors.Count>0).ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>string.IsNullOrWhiteSpace(e.ErrorMessage)?"Dữ liệu không hợp lệ.":e.ErrorMessage).ToArray())}));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o=>{o.SwaggerDoc("v1",new(){Title="RentCar - API trang khách",Version="v1",Description="Lấy mã tại /api/csrf rồi gửi X-CSRF-TOKEN cho thao tác POST. Thời gian đầu vào là giờ Việt Nam (UTC+7)."});o.DocInclusionPredicate((_,d)=>d.RelativePath?.StartsWith("api/")==true);});
        services.AddRateLimiter(o=>
        {
            o.RejectionStatusCode=429;
            o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>
            {
                var p=(ctx.Request.Path.Value??"").ToLowerInvariant();var ip=ctx.Connection.RemoteIpAddress?.ToString()??"local";
                var sensitive=ctx.Request.Method=="POST"&&(p.StartsWith("/tai-khoan/")||p.StartsWith("/api/auth/")||p.StartsWith("/account/login"));
                var submit=ctx.Request.Method=="POST"&&(p.StartsWith("/rental/create")||p=="/api/rental-requests");
                if(!sensitive&&!submit&&!p.StartsWith("/api/"))return RateLimitPartition.GetNoLimiter(ip);
                var category=sensitive?"account":submit?"submit":"api";
                return RateLimitPartition.GetFixedWindowLimiter(ip+":"+category,_=>new FixedWindowRateLimiterOptions {PermitLimit=sensitive?Math.Clamp(configuration.GetValue("CustomerLimits:AccountPerMinute",12),1,1000):submit?20:180,Window=TimeSpan.FromMinutes(1),QueueLimit=0});
            });
            o.AddPolicy("customer-account",ctx=>RateLimitPartition.GetNoLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"local"));
            o.OnRejected=async (c,ct)=>{c.HttpContext.Response.Headers.RetryAfter="60";await c.HttpContext.Response.WriteAsJsonAsync(new {code="RATE_LIMIT",message="Bạn thao tác quá nhanh. Vui lòng thử lại sau một phút."},ct);};
        });
        services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,o=>
        {
            var signingIn=o.Events.OnSigningIn;
            o.Events.OnSigningIn=async c=>
            {
                if(signingIn is not null)await signingIn(c);
                if(c.Principal?.IsInRole("KHACHHANG")!=true)return;
                var db=c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var userId=int.Parse(c.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var hash=await db.Users.AsNoTracking().Where(u=>u.Id==userId).Select(u=>u.PasswordHash).SingleAsync(c.HttpContext.RequestAborted);
                var identity=(ClaimsIdentity)c.Principal.Identity!;
                foreach(var claim in identity.FindAll("customer-password-stamp").ToArray())identity.RemoveClaim(claim);
                identity.AddClaim(new Claim("customer-password-stamp",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)))));
            };
            var original=o.Events.OnValidatePrincipal;
            o.Events.OnValidatePrincipal=async c=>
            {
                if(original is not null)await original(c);
                if(c.Principal?.IsInRole("KHACHHANG")!=true)return;
                var db=c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var id=int.Parse(c.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var hash=await db.Users.AsNoTracking().Where(u=>u.Id==id).Select(u=>u.PasswordHash).SingleAsync(c.HttpContext.RequestAborted);
                var stamp=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
                var stored=c.Principal.FindFirst("customer-password-stamp")?.Value;
                if(stored is not null && stored!=stamp){c.RejectPrincipal();await c.HttpContext.SignOutAsync();return;}
                if(stored is null){((ClaimsIdentity)c.Principal.Identity!).AddClaim(new Claim("customer-password-stamp",stamp));c.ShouldRenew=true;}
            };
            var login=o.Events.OnRedirectToLogin;var denied=o.Events.OnRedirectToAccessDenied;
            o.Events.OnRedirectToLogin=c=>c.Request.Path.StartsWithSegments("/api")?ApiAuth(c.HttpContext,401):login(c);
            o.Events.OnRedirectToAccessDenied=c=>c.Request.Path.StartsWithSegments("/api")?ApiAuth(c.HttpContext,403):denied(c);
        });
    }
    private static Task ApiAuth(HttpContext ctx,int status){ctx.Response.StatusCode=status;return ctx.Response.WriteAsJsonAsync(new{code=status==401?"UNAUTHENTICATED":"FORBIDDEN",message=status==401?"Vui lòng đăng nhập.":"Bạn không có quyền thực hiện thao tác này."});}
    public static async Task CustomerHeaders(HttpContext ctx,RequestDelegate next)
    {
        var p=ctx.Request.Path;
        var action=ctx.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var customer=action?.ControllerTypeInfo.Assembly==typeof(RentCar.Controllers.HomeController).Assembly || p.StartsWithSegments("/api");
        if(!customer){await next(ctx);return;}
        ctx.Response.Headers.XContentTypeOptions="nosniff";ctx.Response.Headers.XFrameOptions="DENY";ctx.Response.Headers["Referrer-Policy"]=p.StartsWithSegments("/tai-khoan")?"no-referrer":"strict-origin-when-cross-origin";
        if(ctx.User.Identity?.IsAuthenticated==true || p.StartsWithSegments("/tai-khoan"))ctx.Response.Headers.CacheControl="no-store";
        // The legacy checkout is an unfinished member stub. Do not let it display a false payment success.
        if(p.Equals(new PathString("/Rental/Checkout"))){ctx.Response.StatusCode=409;await ctx.Response.WriteAsync("Thanh toán đang được tích hợp. Vui lòng theo dõi yêu cầu thuê của bạn.");return;}
        if(p.StartsWithSegments("/api") && HttpMethods.IsPost(ctx.Request.Method))
        {
            try{await ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(ctx);}
            catch(Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
            {ctx.Response.StatusCode=400;await ctx.Response.WriteAsJsonAsync(new{code="CSRF",message="Mã bảo vệ biểu mẫu không hợp lệ. Vui lòng tải lại trang."});return;}
        }
        try{await next(ctx);}
        catch(SqlException ex){ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("CustomerWeb").LogError(ex,"Database unavailable.");ctx.Response.StatusCode=503;await ctx.Response.WriteAsync("Chưa kết nối được dữ liệu. Vui lòng thử lại sau.");}
    }
}
