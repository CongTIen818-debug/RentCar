using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RentCar.Data;
using RentCar.Models;
using RentCar.Models.Database;

namespace RentCar.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController(
    RentCarDbContext database, IPasswordHasher<CustomerAccount> hasher, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginForm { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(form);

        try
        {
            var email = form.Email.Trim();
            var account = await database.CustomerAccounts.Include(user => user.Role)
                .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (account is null || !account.IsActive || account.Role.Code != "KHACHHANG" ||
                string.IsNullOrEmpty(account.PasswordHash))
                return InvalidLogin(form);

            PasswordVerificationResult verification;
            try
            {
                verification = hasher.VerifyHashedPassword(account, account.PasswordHash, form.Password);
            }
            catch (FormatException)
            {
                return InvalidLogin(form);
            }
            if (verification == PasswordVerificationResult.Failed)
                return InvalidLogin(form);

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                account.PasswordHash = hasher.HashPassword(account, form.Password);
                await database.SaveChangesAsync(cancellationToken);
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.FullName),
                new Claim(ClaimTypes.Role, account.Role.Code)
            }, "RentCar");
            await HttpContext.SignInAsync("RentCar", new ClaimsPrincipal(identity));

            return Url.IsLocalUrl(form.ReturnUrl)
                ? LocalRedirect(form.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "Customer login could not reach the database.");
            ModelState.AddModelError("", "Chưa kết nối được hệ thống. Vui lòng thử lại sau.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("RentCar");
        return RedirectToAction("Index", "Home");
    }

    private IActionResult InvalidLogin(LoginForm form)
    {
        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản chưa được kích hoạt.");
        return View("Login", form);
    }
}
