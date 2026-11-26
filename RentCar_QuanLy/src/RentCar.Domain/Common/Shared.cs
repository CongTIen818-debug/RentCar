namespace RentCar.Domain.Common;

/// <summary>Mọi thời điểm của hệ thống dùng múi giờ UTC+7 (SRS mục 2.4.2).</summary>
public static class VietnamTime
{
    // datetime2(0) lưu đến giây: bỏ phần lẻ để thời điểm vừa trả không bị SQL làm tròn thành tương lai.
    public static DateTime Now => FromUtc(new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc));
    public static DateTime ToUtc(DateTime vietnam) => DateTime.SpecifyKind(vietnam.AddHours(-7), DateTimeKind.Utc);
    public static DateTime FromUtc(DateTime utc) => DateTime.SpecifyKind(utc.AddHours(7), DateTimeKind.Unspecified);
}

/// <summary>Vi phạm quy tắc nghiệp vụ. StatusCode dùng để chọn mã HTTP: 400, 401, 403, 404, 409 (SRS mục 7.2.1).</summary>
public sealed class DomainException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Lỗi kiểm tra dữ liệu theo từng trường, hiển thị ngay cạnh ô nhập (UC-01, UC-05).</summary>
public sealed class FieldValidationException(IDictionary<string, string> errors) : Exception("Dữ liệu chưa hợp lệ.")
{
    public IDictionary<string, string> Errors { get; } = new Dictionary<string, string>(errors);
}
