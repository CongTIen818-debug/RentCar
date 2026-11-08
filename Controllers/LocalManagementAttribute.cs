using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RentCar.Controllers;

public sealed class LocalManagementAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var address = context.HttpContext.Connection.RemoteIpAddress;
        if (address is null || !IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address))
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
