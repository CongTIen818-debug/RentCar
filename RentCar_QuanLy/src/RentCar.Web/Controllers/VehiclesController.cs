using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Web.Models;

namespace RentCar.Web.Controllers;

/// <summary>Quản lý đội xe (FR-1.1 đến FR-1.3, UC-04): chỉ Quản trị viên và Nhân viên kinh doanh.</summary>
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Sales)]
public sealed class VehiclesController(IAppDbContext db, IWebHostEnvironment env) : Controller
{
    private const string ImageFolderUrl = "/uploads/vehicles/";
    private const long MaxImageBytes = 5L * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? status, CancellationToken ct)
    {
        var query = db.Vehicles.AsNoTracking().Include(x => x.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.Name.Contains(value) || x.PlateNumber.Contains(value) || x.Brand.Contains(value));
        }
        if (status.HasValue && Enum.IsDefined(typeof(VehicleStatus), status.Value))
            query = query.Where(x => (int)x.Status == status.Value);
        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        return View(await query.OrderBy(x => x.Name).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct) => View("Form", await PrepareAsync(new VehicleForm(), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleForm form, CancellationToken ct)
    {
        await ValidateAsync(form, ct);
        if (!ModelState.IsValid) return View("Form", await PrepareAsync(form, ct));

        // Ghi ảnh trước, lưu CSDL một lần. Nếu lưu lỗi thì xóa ảnh vừa ghi để không còn tệp thừa.
        string? imagePath = null;
        try
        {
            if (form.Image is not null) imagePath = await SaveImageAsync(form.Image, ct);

            db.Vehicles.Add(new Vehicle
            {
                Name = form.Name.Trim(),
                PlateNumber = NormalizePlate(form.PlateNumber),
                Brand = form.Brand.Trim(),
                Model = Clean(form.Model),
                ModelYear = form.ModelYear,
                Color = Clean(form.Color),
                Seats = form.Seats,
                Transmission = form.Transmission,
                FuelType = form.FuelType,
                DailyRate = form.DailyRate,
                WeeklyRate = form.WeeklyRate,
                DepositAmount = form.DepositAmount,
                VehicleTypeId = form.VehicleTypeId,
                Description = Clean(form.Description),
                PrimaryImagePath = imagePath,
                Status = VehicleStatus.Ready
            });
            await db.SaveChangesAsync(ct);

            TempData["Success"] = "Đã thêm xe vào đội xe.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            DeleteImage(imagePath);
            ModelState.AddModelError(nameof(form.PlateNumber), "Biển số đã tồn tại hoặc dữ liệu không phù hợp.");
            return View("Form", await PrepareAsync(form, ct));
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(form.Image), ex.Message);
            return View("Form", await PrepareAsync(form, ct));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var v = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        ViewBag.CurrentImage = v.PrimaryImagePath;
        return View("Form", await PrepareAsync(new VehicleForm
        {
            Id = v.Id, Name = v.Name, PlateNumber = v.PlateNumber, Brand = v.Brand, Model = v.Model,
            ModelYear = v.ModelYear, Color = v.Color, Seats = v.Seats, Transmission = v.Transmission,
            FuelType = v.FuelType, DailyRate = v.DailyRate, WeeklyRate = v.WeeklyRate,
            DepositAmount = v.DepositAmount, VehicleTypeId = v.VehicleTypeId, Description = v.Description
        }, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VehicleForm form, CancellationToken ct)
    {
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == form.Id, ct);
        if (v is null) return NotFound();
        await ValidateAsync(form, ct);
        var oldImage = v.PrimaryImagePath;
        if (!ModelState.IsValid)
        {
            ViewBag.CurrentImage = oldImage;
            return View("Form", await PrepareAsync(form, ct));
        }

        string? newImage = null;
        try
        {
            if (form.Image is not null) newImage = await SaveImageAsync(form.Image, ct);

            v.Name = form.Name.Trim();
            v.PlateNumber = NormalizePlate(form.PlateNumber);
            v.Brand = form.Brand.Trim();
            v.Model = Clean(form.Model);
            v.ModelYear = form.ModelYear;
            v.Color = Clean(form.Color);
            v.Seats = form.Seats;
            v.Transmission = form.Transmission;
            v.FuelType = form.FuelType;
            v.DailyRate = form.DailyRate;
            v.WeeklyRate = form.WeeklyRate;
            v.DepositAmount = form.DepositAmount;
            v.VehicleTypeId = form.VehicleTypeId;
            v.Description = Clean(form.Description);
            if (newImage is not null) v.PrimaryImagePath = newImage;

            await db.SaveChangesAsync(ct);
            if (newImage is not null) DeleteImage(oldImage);

            TempData["Success"] = "Đã cập nhật thông tin xe.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            DeleteImage(newImage);
            ModelState.AddModelError(nameof(form.PlateNumber), "Biển số đã được sử dụng bởi xe khác.");
            ViewBag.CurrentImage = oldImage;
            return View("Form", await PrepareAsync(form, ct));
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(form.Image), ex.Message);
            ViewBag.CurrentImage = oldImage;
            return View("Form", await PrepareAsync(form, ct));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(VehicleStatusForm form, CancellationToken ct)
    {
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == form.Id, ct);
        if (v is null) return NotFound();
        if (!Enum.IsDefined(typeof(VehicleStatus), form.Status))
        {
            TempData["Error"] = "Trạng thái xe không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        var target = (VehicleStatus)form.Status;

        // Ma trận chuyển trạng thái, khớp với trigger trg_xe_ChuyenTrangThai trong CSDL (BR-08).
        var allowed = v.Status switch
        {
            VehicleStatus.Ready => new[] { VehicleStatus.PendingInspection, VehicleStatus.Maintenance, VehicleStatus.Retired },
            VehicleStatus.PendingInspection => new[] { VehicleStatus.Ready, VehicleStatus.Maintenance },
            VehicleStatus.Maintenance => new[] { VehicleStatus.Ready },
            _ => Array.Empty<VehicleStatus>()
        };
        if (target != v.Status && !allowed.Contains(target))
        {
            TempData["Error"] = "Chuyển trạng thái xe không hợp lệ theo quy trình vận hành.";
            return RedirectToAction(nameof(Index));
        }

        if (target is (VehicleStatus.Maintenance or VehicleStatus.Retired) && target != v.Status)
        {
            // Khớp với view vLichXe trong CSDL: hợp đồng Đã xác nhận còn hạn,
            // hoặc hợp đồng Đang thuê mà xe chưa được nhận lại (BR-11).
            var now = VietnamTime.Now;
            var busy = await db.Contracts.AnyAsync(c => c.Lines.Any(l => l.VehicleId == form.Id) &&
                ((c.Status == ContractStatus.Confirmed && c.EndAt > now) ||
                 (c.Status == ContractStatus.Active && (c.Handover == null || c.Handover.ReturnedAt == null))), ct);
            if (busy)
            {
                TempData["Error"] = "Không thể chuyển xe sang Bảo trì/Ngừng hoạt động vì đang có hợp đồng còn hiệu lực.";
                return RedirectToAction(nameof(Index));
            }
        }

        v.Status = target;
        try
        {
            await db.SaveChangesAsync(ct);
            TempData["Success"] = "Đã cập nhật trạng thái xe.";
        }
        catch (DomainException ex)
        {
            // Thông báo từ trigger CSDL đã được chuyển thành DomainException 409 ở AppDbContext.
            TempData["Error"] = ex.Message;
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể đổi trạng thái. Hãy kiểm tra lại lịch thuê của xe.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(VehicleForm f, CancellationToken ct)
    {
        if (!await db.VehicleTypes.AnyAsync(x => x.Id == f.VehicleTypeId, ct))
            ModelState.AddModelError(nameof(f.VehicleTypeId), "Vui lòng chọn loại xe hợp lệ.");
        if (f.Transmission is not ("Tự động" or "Số sàn")) ModelState.AddModelError(nameof(f.Transmission), "Hộp số không hợp lệ.");
        if (f.FuelType is not ("Xăng" or "Dầu" or "Điện" or "Hybrid")) ModelState.AddModelError(nameof(f.FuelType), "Nhiên liệu không hợp lệ.");

        if (f.Image is null) return;
        if (f.Image.Length == 0 || f.Image.Length > MaxImageBytes)
        {
            ModelState.AddModelError(nameof(f.Image), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
            return;
        }
        var ext = Path.GetExtension(f.Image.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png"))
        {
            ModelState.AddModelError(nameof(f.Image), "Chỉ nhận ảnh JPG hoặc PNG.");
            return;
        }
        if (!await HasImageSignatureAsync(f.Image, ext, ct))
            ModelState.AddModelError(nameof(f.Image), "Tệp tải lên không phải ảnh JPG/PNG hợp lệ.");
    }

    private async Task<VehicleForm> PrepareAsync(VehicleForm form, CancellationToken ct)
    {
        ViewBag.VehicleTypes = await db.VehicleTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        ViewBag.IsEdit = form.Id > 0;
        return form;
    }

    /// <summary>Ghi ảnh xe vào wwwroot/uploads/vehicles với tên ngẫu nhiên và trả về đường dẫn web.</summary>
    private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!await HasImageSignatureAsync(file, ext, ct))
            throw new InvalidDataException("Tệp tải lên không phải ảnh JPG/PNG hợp lệ.");

        var folder = Path.Combine(env.WebRootPath, "uploads", "vehicles");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var input = file.OpenReadStream();
        await using var output = System.IO.File.Create(Path.Combine(folder, name));
        await input.CopyToAsync(output, ct);
        return ImageFolderUrl + name;
    }

    /// <summary>Xóa ảnh cũ hoặc ảnh vừa ghi nhưng chưa lưu được. Chỉ xóa tệp nằm trong thư mục ảnh xe.</summary>
    private void DeleteImage(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith(ImageFolderUrl, StringComparison.Ordinal)) return;
        var file = Path.Combine(env.WebRootPath, "uploads", "vehicles", Path.GetFileName(webPath));
        if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
    }

    /// <summary>Kiểm tra chữ ký tệp để không nhận tệp đổi đuôi (FR-1.2b).</summary>
    private static async Task<bool> HasImageSignatureAsync(IFormFile file, string ext, CancellationToken ct)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(), ct);
        var jpg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var png = read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        return ((ext is ".jpg" or ".jpeg") && jpg) || (ext == ".png" && png);
    }

    private static string NormalizePlate(string value) => string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
