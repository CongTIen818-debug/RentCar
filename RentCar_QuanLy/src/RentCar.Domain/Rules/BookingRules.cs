using RentCar.Domain.Common;

namespace RentCar.Domain.Rules;

/// <summary>Các hằng số và quy tắc thời gian thuê (SRS mục 3.7).</summary>
public static class BookingRules
{
    public const int MinAge = 21;                 // BR-12
    public const int MinRentalHours = 4;          // BR-12
    public const int MaxRentalDays = 30;          // BR-12
    public const int HoldHours = 24;              // BR-15
    public const int NoShowHours = 4;             // UC-03 A4
    public const int CancelFreeHours = 48;        // BR-14
    public const int LateGraceMinutes = 120;      // SP-01 (mẫu)
    public const int KmPerDayAllowance = 200;     // SP-02 (mẫu)
    public const decimal KmUnitPrice = 5000m;     // SP-02 (mẫu)

    /// <summary>Ánh xạ hạng GPLX mẫu: hạng cao hơn đáp ứng hạng thấp hơn (mục 3.7).</summary>
    private static readonly string[] LicenseRank = ["B", "C", "D", "E"];
    /// <summary>Các hạng GPLX hợp lệ theo thứ tự tăng dần.</summary>
    public static IReadOnlyList<string> LicenseClasses => LicenseRank;

    /// <summary>BR-12: người thuê phải đủ 21 tuổi tính đến thời điểm nhận xe.</summary>
    public static bool IsOldEnough(DateOnly birthDate, DateTime atTime)
    {
        var at = DateOnly.FromDateTime(atTime);
        var age = at.Year - birthDate.Year;
        if (birthDate > at.AddYears(-age)) age--;
        return age >= MinAge;
    }

    /// <summary>BR-03c: GPLX còn hiệu lực đến hết thời gian thuê và đúng hạng tối thiểu.</summary>
    public static bool LicenseCovers(string licenseClass, string requiredClass, DateOnly expiry, DateTime endAt)
    {
        var have = Array.IndexOf(LicenseRank, licenseClass.Trim().ToUpperInvariant());
        var need = Array.IndexOf(LicenseRank, requiredClass.Trim().ToUpperInvariant());
        return have >= 0 && need >= 0 && have >= need && expiry >= DateOnly.FromDateTime(endAt);
    }

    /// <summary>BR-02, BR-12: kiểm tra khoảng thời gian thuê hợp lệ.</summary>
    public static void ValidateWindow(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
            throw new DomainException("Thời điểm trả xe phải sau thời điểm nhận xe (BR-02).");
        if ((endAt - startAt).TotalMinutes < MinRentalHours * 60)
            throw new DomainException($"Thời gian thuê tối thiểu là {MinRentalHours} giờ (BR-12).");
        if (endAt > startAt.AddDays(MaxRentalDays))
            throw new DomainException($"Thời gian thuê tối đa là {MaxRentalDays} ngày (BR-12).");
    }
}
