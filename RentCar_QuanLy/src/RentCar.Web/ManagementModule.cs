using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Application.Accounts;
using RentCar.Application.Auth;
using RentCar.Application.Catalog;
using RentCar.Application.Rentals;
using RentCar.Infrastructure.Persistence;
using RentCar.Infrastructure.Services;
using RentCar.Shared;
using RentCar.Web.Hosting;

namespace RentCar.Web;

public static class ManagementModule
{
    public static IServiceCollection AddRentCarManagement(this IServiceCollection services, IConfiguration configuration)
    {
// Tầng dữ liệu: DbContext ánh xạ vào CSDL RentCarDb (SQL Server 2019 trở lên).
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("RentCar")));
services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

// Gửi email: dùng SMTP khi đã cấu hình Smtp:Host; chưa cấu hình thì chạy chế độ mô phỏng (không gửi).
services.AddSingleton<IEmailSender>(sp =>
{
    var smtp = configuration.GetSection("Smtp");
    if (string.IsNullOrWhiteSpace(smtp["Host"])) return new NullEmailSender();
    if (string.IsNullOrWhiteSpace(smtp["From"]))
        throw new InvalidOperationException("Smtp:From là bắt buộc khi đã cấu hình Smtp:Host.");

    var settings = new SmtpSettings(
        Host: smtp["Host"]!,
        Port: int.TryParse(smtp["Port"], out var port) ? port : 587,
        EnableSsl: bool.TryParse(smtp["EnableSsl"], out var ssl) ? ssl : true,
        From: smtp["From"]!,
        UserName: smtp["UserName"],
        Password: smtp["Password"]);
    return new SmtpEmailSender(settings, sp.GetRequiredService<ILogger<SmtpEmailSender>>());
});

// Tầng nghiệp vụ.
services.AddScoped<AuthService>();
services.AddScoped<StaffAccountService>();
services.AddScoped<StaffInvitationService>();
services.AddScoped<AccountSecurityService>();
services.AddScoped<ProfileService>();
services.AddScoped<CatalogService>();
services.AddScoped<RentCar.Application.Dashboard.DashboardService>();
services.AddHostedService<RentCar.Web.Services.HoldExpirationWorker>();
services.AddScoped<RentalService>();
services.AddScoped<RentCar.Application.Handover.HandoverService>();

services.AddSharedAuthenticationKeys(configuration);

// Xác thực bằng cookie phiên, dùng chung với trang chủ.
services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        SharedAuthentication.ConfigureCookie(options);
        // Kiểm tra lại tài khoản ở mỗi yêu cầu: khóa tài khoản hoặc đổi vai trò có hiệu lực ngay,
        // không chờ hết phiên (FR-6.3, FR-6.4).
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
            var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleInCookie = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var current = int.TryParse(idText, out var userId)
                ? await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, RoleCode = u.Role.Code })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted)
                : null;
            if (current is null || !current.IsActive || current.RoleCode != roleInCookie)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

        services.AddAuthorization();
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(Controllers.AccountController).Assembly)
            .AddRazorOptions(options => options.ViewLocationExpanders.Insert(0, new ManagementViewLocationExpander()));
        return services;
    }
}
