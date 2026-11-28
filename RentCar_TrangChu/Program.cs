using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Web;
using RentCar.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRentCarManagement(builder.Configuration);
builder.Services.AddCustomerFeatures(builder.Configuration);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
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
app.UseRateLimiter();
app.Use(CustomerWebFeatures.CustomerHeaders);
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(o=>o.UseRequestInterceptor("function(request) { if(request.method === 'GET') return request; return fetch('/api/csrf',{credentials:'same-origin'}).then(function(r){return r.json();}).then(function(t){request.headers['X-CSRF-TOKEN']=t.token;return request;}); }")); }

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
app.MapGet("/health", async (RentCarDbContext database,CancellationToken ct) =>
{
    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(3));
    try {return await database.Database.CanConnectAsync(timeout.Token)?Results.Json(new{status="ok"}):Results.Json(new{status="unavailable"},statusCode:503);}
    catch(Exception ex) when(ex is OperationCanceledException or Microsoft.Data.SqlClient.SqlException){return Results.Json(new{status="unavailable"},statusCode:503);}
}).AllowAnonymous();
app.Run();
