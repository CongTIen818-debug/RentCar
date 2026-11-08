using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;

namespace RentCar.Controllers
{
    public class HomeController(ILogger<HomeController> logger, RentCarDbContext database) : Controller
    {
        public async Task<IActionResult> Index(string[] types, int[] seats, decimal? maxPrice,
            int? pickupId, int? returnId, DateTime? pickupAt, DateTime? returnAt, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || maxPrice is < 0)
                return BadRequest("Bộ lọc không hợp lệ.");

            var vehicles = database.Vehicles.AsNoTracking()
                .Where(vehicle => vehicle.Status != "Ngừng hoạt động" && vehicle.Status != "Bảo trì");

            try
            {
                var locations = await database.PickupLocations.AsNoTracking()
                    .Where(location => location.IsActive).OrderBy(location => location.Name)
                    .ToListAsync(cancellationToken);
                var isSearch = pickupAt.HasValue || returnAt.HasValue || pickupId.HasValue || returnId.HasValue;
                string? searchError = null;
                if (isSearch)
                {
                    if (!locations.Any(location => location.Id == pickupId) ||
                        (returnId.HasValue && !locations.Any(location => location.Id == returnId)))
                        searchError = "Vui lòng chọn điểm giao nhận đang hoạt động.";
                    else if (pickupAt is not DateTime pickup || returnAt is not DateTime dropoff ||
                        dropoff - pickup < TimeSpan.FromHours(4) || dropoff - pickup > TimeSpan.FromDays(30))
                        searchError = "Thời gian thuê phải từ 4 giờ đến 30 ngày.";
                    else
                    {
                        var start = pickup.AddHours(-7);
                        var end = dropoff.AddHours(-7);
                        var now = DateTime.UtcNow;
                        vehicles = vehicles.Where(vehicle =>
                            !vehicle.RentalRequests.Any(request => request.Status == "Chờ xử lý" &&
                                request.ExpiresAt > now && request.StartAt < end && start < request.EndAt) &&
                            !vehicle.RentalContracts.Any(contract => contract.StartAt < end &&
                                ((contract.Status == "Đang thuê" && contract.ActualReturnAt == null &&
                                    (start < contract.EndAt || contract.EndAt <= now)) ||
                                 (start < contract.EndAt &&
                                    (contract.Status == "Đã xác nhận" ||
                                     (contract.Status == "Chờ xác nhận" && contract.ExpiresAt > now))))) &&
                            !vehicle.MaintenanceRecords.Any(maintenance =>
                                maintenance.Status != "Đã hủy" &&
                                maintenance.StartAt < end && start < maintenance.EndAt));
                    }
                }

                var typeOptions = await vehicles.GroupBy(vehicle => vehicle.VehicleType)
                    .OrderBy(group => group.Key)
                    .Select(group => new FilterOption<string>(group.Key, group.Count()))
                    .ToListAsync(cancellationToken);
                var seatOptions = await vehicles.GroupBy(vehicle => vehicle.Seats)
                    .OrderBy(group => group.Key)
                    .Select(group => new FilterOption<byte>(group.Key, group.Count()))
                    .ToListAsync(cancellationToken);
                var highestPrice = await vehicles.MaxAsync(vehicle => (decimal?)vehicle.DailyRate, cancellationToken) ?? 0;
                var selectedPrice = Math.Min(maxPrice ?? highestPrice, highestPrice);

                if (types.Length > 0)
                    vehicles = vehicles.Where(vehicle => types.Contains(vehicle.VehicleType));
                if (seats.Length > 0)
                    vehicles = vehicles.Where(vehicle => seats.Contains((int)vehicle.Seats));
                vehicles = vehicles.Where(vehicle => vehicle.DailyRate <= selectedPrice);

                var count = await vehicles.CountAsync(cancellationToken);
                var results = searchError is null
                    ? await vehicles.OrderBy(vehicle => vehicle.DailyRate).ThenBy(vehicle => vehicle.Id)
                        .Select(vehicle => new VehicleSearchResult(vehicle.Brand, vehicle.Model, vehicle.VehicleType,
                            vehicle.Seats, vehicle.Transmission, vehicle.DailyRate,
                            vehicle.VehicleImages.OrderByDescending(image => image.IsPrimary).ThenBy(image => image.Id)
                                .Select(image => image.ImageUrl).FirstOrDefault()))
                        .Take(12).ToListAsync(cancellationToken)
                    : [];
                return View(new VehicleFilterViewModel(typeOptions, seatOptions, types, seats, highestPrice,
                    selectedPrice, count, locations, pickupId, returnId, pickupAt, returnAt, isSearch, searchError, results));
            }
            catch (SqlException exception)
            {
                logger.LogWarning(exception, "Could not load vehicle filters.");
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
