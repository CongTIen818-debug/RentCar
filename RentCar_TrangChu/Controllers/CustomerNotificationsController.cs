using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentCar.Domain.Common;
using RentCar.Services;
namespace RentCar.Controllers;
[Route("thong-bao")][Authorize(Roles="KHACHHANG")][AutoValidateAntiforgeryToken][ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CustomerNotificationsController(CustomerNotifications notices):Controller
{
 private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet("")]public async Task<IActionResult> Index(int page=1,CancellationToken ct=default)=>View(await notices.ListAsync(UserId,page,ct));
 [HttpPost("da-doc")]public async Task<IActionResult> Read(string key,int page=1,CancellationToken ct=default)
 {if(string.IsNullOrWhiteSpace(key))return BadRequest();try{await notices.MarkReadAsync(UserId,key,ct);}catch(DomainException ex){return StatusCode(ex.StatusCode);}return RedirectToAction(nameof(Index),new{page});}
}
