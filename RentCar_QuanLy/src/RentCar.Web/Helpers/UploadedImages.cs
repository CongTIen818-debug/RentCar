using RentCar.Domain.Common;

namespace RentCar.Web.Helpers;

/// <summary>
/// Kiểm tra và lưu ảnh biên bản: chỉ JPG hoặc PNG, tối đa 5 MB mỗi ảnh và 10 ảnh mỗi lần (UC-04 A6, FR-4.1).
/// Kiểm tra cả chữ ký tệp để không nhận tệp đổi đuôi.
/// </summary>
public static class UploadedImages
{
    public const int MaxFiles = 10;
    public const long MaxBytes = 5L * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public static async Task<List<string>> SaveAsync(IReadOnlyCollection<IFormFile>? files, IWebHostEnvironment env,
        int contractId, CancellationToken ct)
    {
        var list = files?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (list.Count == 0) return new List<string>();
        if (list.Count > MaxFiles)
            throw new DomainException($"Mỗi lần tải tối đa {MaxFiles} ảnh (A6).");

        // Kiểm tra toàn bộ trước khi ghi, để không lưu dở dang khi một tệp sai.
        foreach (var file in list)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new DomainException("Chỉ nhận ảnh JPG hoặc PNG (A6).");
            if (file.Length > MaxBytes)
                throw new DomainException("Mỗi ảnh tối đa 5 MB (A6).");
            if (!await HasImageSignatureAsync(file, ct))
                throw new DomainException("Tệp không phải ảnh JPG hoặc PNG hợp lệ (A6).");
        }

        var folder = Path.Combine(env.WebRootPath, "uploads", "handover", contractId.ToString());
        Directory.CreateDirectory(folder);

        var saved = new List<string>();
        foreach (var file in list)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"{Guid.NewGuid():N}{ext}";
            await using var stream = File.Create(Path.Combine(folder, name));
            await file.CopyToAsync(stream, ct);
            saved.Add($"/uploads/handover/{contractId}/{name}");
        }
        return saved;
    }

    private static async Task<bool> HasImageSignatureAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;   // JPEG
        return read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47; // PNG
    }
}
