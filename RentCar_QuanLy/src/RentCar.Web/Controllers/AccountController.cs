using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Application.Accounts;
using RentCar.Application.Auth;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>UC-01 đăng nhập, UC-05 đăng ký, UC-07 đổi/quên mật khẩu, FR-6.2a hồ sơ khách hàng.</summary>
public sealed class AccountController(
    AuthService auth,
    AccountSecurityService security,
    ProfileService profile,
    StaffInvitationService invitations,
    IWebHostEnvironment env) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return AfterLogin(User.FindFirstValue(ClaimTypes.Role) ?? "", returnUrl);
        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    private IActionResult AfterLogin(string roleCode, string? returnUrl)
    {
        var safeReturn = Url.IsLocalUrl(returnUrl)
            && !returnUrl!.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase);
        if (roleCode == RoleCodes.Customer)
            return safeReturn ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
        if (safeReturn && returnUrl!.StartsWith("/QuanLy", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(returnUrl);
        return roleCode == RoleCodes.Handover
            ? RedirectToAction("Index", "Handover")
            : RedirectToAction("Index", "Staff");
    }

    [HttpGet]
    public IActionResult AccessDenied() => StatusCode(StatusCodes.Status403Forbidden, "Tài khoản của bạn không có quyền truy cập trang này.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        try
        {
            var user = await auth.SignInAsync(form.Email, form.Password, ct);
            await SignInAsync(user);

            return AfterLogin(user.Role.Code, form.ReturnUrl);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterForm { LicenseClass = "B" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        try
        {
            await auth.RegisterCustomerAsync(new RegisterCustomerRequest(
                form.FullName, form.BirthDate, form.Phone, form.Email, form.IdNumber, form.Address,
                form.LicenseNumber, form.LicenseClass, form.LicenseExpiry, form.Password, form.ConfirmPassword), ct);

            TempData["Success"] = "Đăng ký thành công. Hãy đăng nhập để tra cứu xe và gửi yêu cầu thuê.";
            return RedirectToAction(nameof(Login));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors)
                ModelState.AddModelError(key, message);
            return View(form);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Bạn đã đăng xuất.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordForm());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            await security.ChangePasswordAsync(UserId, form.CurrentPassword, form.NewPassword, form.ConfirmPassword, ct);
            TempData["Success"] = "Đã đổi mật khẩu. Lần đăng nhập sau hãy dùng mật khẩu mới.";
            return RedirectToAction("Index", "Home");
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            return View(form);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordForm());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        var code = await security.RequestResetCodeAsync(form.Email, ct);
        // Mã chỉ hiện trên màn hình khi chưa cấu hình SMTP và đang chạy môi trường Development.
        // Mã không được ghi vào log. Thông báo bên dưới giống hệt dù email có tồn tại hay không.
        if (code is not null && env.IsDevelopment()) TempData["DevResetCode"] = code;

        TempData["Success"] = "Nếu email đã đăng ký, hệ thống đã gửi mã đặt lại mật khẩu đến email đó. Mã có hiệu lực trong 30 phút.";
        return RedirectToAction(nameof(ResetPassword), new { email = form.Email });
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email)
    {
        ViewBag.DevCode = TempData["DevResetCode"] as string;
        return View(new ResetPasswordForm { Email = email ?? "" });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            await security.ResetPasswordAsync(form.Email, form.Code, form.NewPassword, form.ConfirmPassword, ct);
            TempData["Success"] = "Đã đặt lại mật khẩu. Hãy đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            return View(form);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult StaffRegister() => View(new StaffRegisterForm());

    /// <summary>v4: nhân viên tự tạo tài khoản bằng mã do quản trị viên cấp. Tài khoản dùng được ngay.</summary>
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> StaffRegister(StaffRegisterForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(ClearPasswords(form));
        try
        {
            await invitations.RedeemAsync(new StaffRedeemRequest(form.Code, form.Email, form.Phone,
                form.Password, form.ConfirmPassword), ct);

            TempData["Success"] = "Đăng ký nhân viên thành công. Hãy đăng nhập bằng email và mật khẩu vừa tạo.";
            return RedirectToAction(nameof(Login));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            return View(ClearPasswords(form));
        }
    }

    /// <summary>Không trả mật khẩu về lại trình duyệt khi form có lỗi.</summary>
    private static StaffRegisterForm ClearPasswords(StaffRegisterForm form)
    {
        form.Password = "";
        form.ConfirmPassword = "";
        return form;
    }

    [HttpGet, Authorize(Roles = RoleCodes.Customer)]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var data = await profile.GetAsync(UserId, ct);
        SetProfileViewBag(data);
        return View(new ProfileForm
        {
            FullName = data.FullName,
            Phone = data.Phone,
            Address = data.Address,
            LicenseNumber = data.LicenseNumber ?? "",
            LicenseClass = data.LicenseClass ?? "B",
            LicenseExpiry = data.LicenseExpiry
        });
    }

    [HttpPost, Authorize(Roles = RoleCodes.Customer), ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetProfileViewBag(await profile.GetAsync(UserId, ct));
            return View(form);
        }
        try
        {
            await profile.UpdateAsync(UserId, new ProfileUpdateRequest(form.FullName, form.Phone, form.Address,
                form.LicenseNumber, form.LicenseClass, form.LicenseExpiry), ct);
            TempData["Success"] = "Đã cập nhật hồ sơ và GPLX.";
            return RedirectToAction(nameof(Profile));
        }
        catch (FieldValidationException ex)
        {
            foreach (var (key, message) in ex.Errors) ModelState.AddModelError(key, message);
            SetProfileViewBag(await profile.GetAsync(UserId, ct));
            return View(form);
        }
    }

    private void SetProfileViewBag(ProfileData data)
    {
        ViewBag.Email = data.Email;
        ViewBag.BirthDateText = data.BirthDate?.ToString("dd/MM/yyyy") ?? "";
        ViewBag.IdMasked = MaskId(data.IdNumber);
    }

    /// <summary>NFR-13: giao diện chỉ hiện 4 số cuối của số giấy tờ.</summary>
    private static string MaskId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return id.Length <= 4 ? id : new string('•', id.Length - 4) + id[^4..];
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Code)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
    }
}
