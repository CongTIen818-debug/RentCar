using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace RentCar.Web.Hosting;

// Controller của thư viện quản lý dùng bộ View/Layout riêng; controller trang chủ dùng View/Layout trang chủ.
public sealed class ManagementViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var action = context.ActionContext.ActionDescriptor as ControllerActionDescriptor;
        context.Values["ManagementModule"] = action?.ControllerTypeInfo.Assembly == typeof(Controllers.AccountController).Assembly ? "1" : "0";
    }
    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations) =>
        context.Values["ManagementModule"] == "1"
            ? ["/Areas/QuanLy/Views/{1}/{0}.cshtml", "/Areas/QuanLy/Views/Shared/{0}.cshtml"]
            : viewLocations;
}
