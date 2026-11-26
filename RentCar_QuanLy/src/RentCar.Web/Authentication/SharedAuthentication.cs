using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RentCar.Shared;

// Cấu hình phiên đăng nhập duy nhất; nghiệp vụ đăng nhập ở RentCar.Application.
public static class SharedAuthentication
{
    public static void AddSharedAuthenticationKeys(this IServiceCollection services, IConfiguration configuration)
    {
        var path = configuration["SharedAuthentication:KeyRingPath"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RentCar", "SharedAuthenticationKeys");
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(path))
            .SetApplicationName("RentCar.SharedAuthentication.v1");
    }

    public static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "RentCar.SharedAuth";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    }

}
