using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<RentCar.Models.Database.CustomerAccount>,
    Microsoft.AspNetCore.Identity.PasswordHasher<RentCar.Models.Database.CustomerAccount>>();
builder.Services.AddAuthentication("RentCar").AddCookie("RentCar", options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.Name = ".RentCar.CustomerAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnValidatePrincipal = async context =>
    {
        var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var database = context.HttpContext.RequestServices.GetRequiredService<RentCarDbContext>();
        if (!int.TryParse(id, out var customerId) ||
            !await database.CustomerAccounts.AsNoTracking().AnyAsync(user =>
                user.Id == customerId && user.IsActive && user.Role.Code == "KHACHHANG",
                context.HttpContext.RequestAborted))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync("RentCar");
        }
    };
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
builder.Services.AddDbContext<RentCarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RentCar")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
