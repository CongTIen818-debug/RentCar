using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRentCarManagement(builder.Configuration);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RentCar.Services.VehicleCatalog>();
builder.Services.AddScoped<RentCar.Services.RentalSelection>();
builder.Services.AddScoped<RentCar.Services.RentalPricing>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RentCar.Services.CustomerRequests>();
builder.Services.AddScoped<RentCar.Services.CustomerRequestSubmission>();
builder.Services.AddScoped<RentCar.Services.CustomerContracts>();
builder.Services.AddDbContext<RentCarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RentCar")));
builder.Services.AddHttpsRedirection(options => options.HttpsPort = builder.Configuration.GetValue<int>("HttpsPort", 7246));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
// Legacy management uploads remain available; new uploads use the one host's wwwroot.
var legacyUploads = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../RentCar_QuanLy/src/RentCar.Web/wwwroot/uploads"));
if (Directory.Exists(legacyUploads))
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(legacyUploads),
        RequestPath = "/uploads"
    });
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/QuanLy", (System.Security.Claims.ClaimsPrincipal user) =>
    Results.LocalRedirect(user.IsInRole("GIAONHAN") ? "/QuanLy/Handover" : "/QuanLy/Staff")).RequireAuthorization();
app.MapControllerRoute(
    name: "management",
    pattern: "QuanLy/{controller}/{action=Index}/{id?}",
    constraints: new { controller = "^(?:Catalog|Dashboard|Staff|StaffAccounts|Vehicles|Handover|Maintenance|Pricing|Rentals)$" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    constraints: new { controller = "^(?:Home|Cars|Rental|Services|Account)$" });
app.Run();
