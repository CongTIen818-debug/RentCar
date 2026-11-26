using System.Text.Json;

namespace RentCar.Services;

public class RentalSelection(IHttpContextAccessor accessor)
{
    private ISession Session => accessor.HttpContext!.Session;
    // Một địa điểm dùng chung cho nhận và trả; khi lập yêu cầu dùng cho cả idDiemNhan và idDiemTra.
    public int? LocationId() => Session.GetInt32("RentalLocationId");
    public void SetLocation(int locationId) => Session.SetInt32("RentalLocationId", locationId);
    public List<int> Read() =>
        JsonSerializer.Deserialize<List<int>>(Session.GetString("RentalSelection") ?? "[]") ?? [];
    public HashSet<int> DriverIds() =>
        JsonSerializer.Deserialize<HashSet<int>>(Session.GetString("RentalDrivers") ?? "[]") ?? [];
    public void Add(int id, bool withDriver = false)
    {
        var ids = Read();
        if (!ids.Contains(id)) ids.Add(id);
        Session.SetString("RentalSelection", JsonSerializer.Serialize(ids));
        SetDriver(id, withDriver);
    }
    public void SetDriver(int id, bool withDriver)
    {
        if (!Read().Contains(id)) return;
        var drivers = DriverIds();
        if (withDriver) drivers.Add(id); else drivers.Remove(id);
        Session.SetString("RentalDrivers", JsonSerializer.Serialize(drivers));
    }
    public void Remove(int id)
    {
        SetDriver(id, false);
        var ids = Read();
        ids.RemoveAll(value => value == id);
        Session.SetString("RentalSelection", JsonSerializer.Serialize(ids));
    }
}
