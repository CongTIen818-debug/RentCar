using Microsoft.AspNetCore.Mvc;

namespace RentCar.Controllers;

[LocalManagement]
public class ManagementController : Controller
{
    public IActionResult Index() => View();
}
