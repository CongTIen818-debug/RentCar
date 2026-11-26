using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RentCar.Infrastructure.Persistence;

/// <summary>
/// Chuyển đổi cho các cột TINYINT (soCho, mucNhienLieuGiao, mucNhienLieuNhan).
/// SqlClient không đọc được TINYINT vào kiểu int (lỗi InvalidCastException: Byte -> Int32),
/// nên EF đọc qua byte và chuyển sang int trong ứng dụng. checked() báo lỗi khi giá trị ngoài 0-255,
/// thay vì cắt bớt âm thầm.
/// </summary>
internal static class TinyIntConverters
{
    public static readonly ValueConverter<int, byte> Required = new(
        v => checked((byte)v),
        v => (int)v);

    public static readonly ValueConverter<int?, byte?> Optional = new(
        v => v.HasValue ? (byte?)checked((byte)v.Value) : (byte?)null,
        v => v.HasValue ? (int?)v.Value : (int?)null);
}
