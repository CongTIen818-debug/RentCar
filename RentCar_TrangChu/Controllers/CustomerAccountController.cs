using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentCar.Application.Accounts;
using RentCar.Domain.Common;
using RentCar.Services;
using RentCar.Web.Models;
namespace RentCar.Controllers;
public sealed class PrivacyForm
{
    [Required(ErrorMessage="Vui lòng nhập mật khẩu hiện tại.")] public string Password {get;set;}="";
    public bool Confirm {get;set;}
}
[Route("tai-khoan")]
[RequestSizeLimit(16384)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CustomerAccountController(ProfileService profile,AccountSecurityService security,CustomerRecovery recovery,CustomerPrivacy privacy) : Controller
{
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task Metadata(CancellationToken ct)
    {
        var data=await profile.GetAsync(UserId,ct);ViewData["Email"]=data.Email;ViewData["BirthDate"]=data.BirthDate?.ToString("dd/MM/yyyy");
        ViewData["IdMasked"]=string.IsNullOrEmpty(data.IdNumber)?"Chưa có":new string('•',Math.Max(0,data.IdNumber.Length-4))+data.IdNumber[^Math.Min(4,data.IdNumber.Length)..];
    }
    [HttpGet("ho-so"),Authorize(Roles="KHACHHANG")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var data=await profile.GetAsync(UserId,ct);await Metadata(ct);
        return View(new ProfileForm {FullName=data.FullName,Phone=data.Phone,Address=data.Address,LicenseNumber=data.LicenseNumber??"",LicenseClass=data.LicenseClass??"B",LicenseExpiry=data.LicenseExpiry});
    }
    [HttpPost("ho-so"),Authorize(Roles="KHACHHANG")]
    public async Task<IActionResult> Profile(ProfileForm form,CancellationToken ct)
    {
        if(ModelState.IsValid) try
        {
            await profile.UpdateAsync(UserId,new ProfileUpdateRequest(form.FullName,form.Phone,form.Address,form.LicenseNumber,form.LicenseClass,form.LicenseExpiry),ct);
            var identity=new ClaimsIdentity(User.Claims.Where(c=>c.Type!=ClaimTypes.Name),CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name,form.FullName.Trim()));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity));
            TempData["CustomerMessage"]="Đã cập nhật hồ sơ và GPLX.";return RedirectToAction(nameof(Profile));
        }
        catch(FieldValidationException ex){foreach(var e in ex.Errors)ModelState.AddModelError(e.Key,e.Value);}
        await Metadata(ct);return View(form);
    }
    [HttpGet("doi-mat-khau"),Authorize(Roles="KHACHHANG")] public IActionResult ChangePassword()=>View(new ChangePasswordForm());
    [HttpPost("doi-mat-khau"),Authorize(Roles="KHACHHANG"),EnableRateLimiting("customer-account")]
    public async Task<IActionResult> ChangePassword(ChangePasswordForm form,CancellationToken ct)
    {
        if(ModelState.IsValid)try {await security.ChangePasswordAsync(UserId,form.CurrentPassword,form.NewPassword,form.ConfirmPassword,ct);await HttpContext.SignOutAsync();TempData["Success"]="Đã đổi mật khẩu. Vui lòng đăng nhập lại.";return RedirectToAction("Login","Account");}
        catch(FieldValidationException ex){foreach(var e in ex.Errors)ModelState.AddModelError(e.Key,e.Value);}
        form.CurrentPassword="";form.NewPassword="";form.ConfirmPassword="";ModelState.SetModelValue("CurrentPassword",null,null);ModelState.SetModelValue("NewPassword",null,null);ModelState.SetModelValue("ConfirmPassword",null,null);return View(form);
    }
    [HttpGet("quen-mat-khau"),AllowAnonymous] public IActionResult ForgotPassword()=>View(new ForgotPasswordForm());
    [HttpPost("quen-mat-khau"),AllowAnonymous,EnableRateLimiting("customer-account")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form,CancellationToken ct)
    {
        if(!ModelState.IsValid)return View(form);
        var preview=await recovery.RequestAsync(form.Email,ct);ViewData["ResetPreview"]=preview;
        ViewData["Requested"]=true;return View(new ForgotPasswordForm());
    }
    [HttpGet("dat-lai-mat-khau"),AllowAnonymous] public IActionResult ResetPassword(string? email,string? code)=>View(new ResetPasswordForm {Email=email??"",Code=code??""});
    [HttpPost("dat-lai-mat-khau"),AllowAnonymous,EnableRateLimiting("customer-account")]
    public async Task<IActionResult> ResetPassword(ResetPasswordForm form,CancellationToken ct)
    {
        if(ModelState.IsValid)try {await recovery.ResetAsync(form.Email,form.Code,form.NewPassword,form.ConfirmPassword,ct);TempData["Success"]="Đã đặt lại mật khẩu. Vui lòng đăng nhập.";return RedirectToAction("Login","Account");}
        catch(FieldValidationException ex){foreach(var e in ex.Errors)ModelState.AddModelError(e.Key,e.Value);}
        form.NewPassword="";form.ConfirmPassword="";ModelState.SetModelValue("NewPassword",null,null);ModelState.SetModelValue("ConfirmPassword",null,null);return View(form);
    }
    [HttpGet("quyen-rieng-tu"),Authorize(Roles="KHACHHANG")]public IActionResult Privacy()=>View(new PrivacyForm());
    [HttpPost("quyen-rieng-tu"),Authorize(Roles="KHACHHANG"),EnableRateLimiting("customer-account")]
    public async Task<IActionResult> Privacy(PrivacyForm form,CancellationToken ct)
    {
        if(!form.Confirm)ModelState.AddModelError("Confirm","Vui lòng xác nhận đã hiểu việc ẩn danh tài khoản.");
        if(ModelState.IsValid)try {await privacy.AnonymizeAsync(UserId,form.Password,ct);await HttpContext.SignOutAsync();TempData["CustomerMessage"]="Thông tin tài khoản đã được ẩn danh. Hợp đồng và giao dịch được giữ theo chính sách lưu trữ.";return RedirectToAction("Index","Home");}
        catch(FieldValidationException ex){foreach(var e in ex.Errors)ModelState.AddModelError(e.Key,e.Value);}
        catch(DomainException ex){ModelState.AddModelError("",ex.Message);}
        form.Password="";ModelState.SetModelValue("Password",null,null);return View(form);
    }
}
