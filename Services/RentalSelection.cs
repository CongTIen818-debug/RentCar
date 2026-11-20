using System.Text.Json;

namespace RentCar.Services;

public class RentalSelection(IHttpContextAccessor accessor)
{
    private ISession Session => accessor.HttpContext!.Session;

    public List<int> Read() =>
        JsonSerializer.Deserialize<List<int>>(Session.GetString("RentalSelection") ?? "[]") ?? [];

    public void Add(int id)
    {
        var ids = Read();
        if (!ids.Contains(id))
            ids.Add(id);
        Session.SetString("RentalSelection", JsonSerializer.Serialize(ids));
    }

    public void Remove(int id)
    {
        var ids = Read();
        ids.RemoveAll(value => value == id);
        Session.SetString("RentalSelection", JsonSerializer.Serialize(ids));
    }
}

