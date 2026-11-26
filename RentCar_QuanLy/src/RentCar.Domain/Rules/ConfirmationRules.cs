using RentCar.Domain.Entities;
using RentCar.Domain.Enums;

namespace RentCar.Domain.Rules;

public sealed record ConfirmationCheck(string Label, bool Passed);

/// <summary>
/// Danh sách kiểm tra trước khi xác nhận hợp đồng (BR-01, BR-03, BR-12).
/// Hàm thuần: nhận dữ liệu đã nạp sẵn, không truy vấn CSDL, nên kiểm thử được (NFR-07).
/// </summary>
public static class ConfirmationRules
{
    public static IReadOnlyList<ConfirmationCheck> Evaluate(RentalContract contract, decimal depositPaid, bool hasClash)
    {
        var customer = contract.Customer;
        var lines = contract.Lines;
        var license = customer.DriverLicense;

        var hasProfile = !string.IsNullOrWhiteSpace(customer.FullName)
                         && !string.IsNullOrWhiteSpace(customer.Phone)
                         && !string.IsNullOrWhiteSpace(customer.Email)
                         && !string.IsNullOrWhiteSpace(customer.IdNumber);

        var isOldEnough = customer.BirthDate is { } birth && BookingRules.IsOldEnough(birth, contract.StartAt);

        var licenseOk = lines.Count > 0 && lines.All(l => l.WithDriver ||
            (license is not null && BookingRules.LicenseCovers(license.LicenseClass, l.Vehicle.VehicleType.MinLicenseClass,
                license.ExpiryDate, contract.EndAt)));

        return
        [
            new ConfirmationCheck("Khách có đủ họ tên, số điện thoại, email và giấy tờ tùy thân (BR-03b)", hasProfile),
            new ConfirmationCheck("Khách đủ 21 tuổi tại thời điểm nhận xe (BR-12)", isOldEnough),
            new ConfirmationCheck("GPLX còn hiệu lực đến hết thời gian thuê và đúng hạng xe (BR-03c)", licenseOk),
            new ConfirmationCheck("Đã ghi nhận đủ tiền đặt cọc (BR-14)", depositPaid >= contract.DepositAmount),
            new ConfirmationCheck("Xe đang ở trạng thái Sẵn sàng (BR-08)", lines.Count > 0 && lines.All(l => l.Vehicle.Status == VehicleStatus.Ready)),
            new ConfirmationCheck("Xe không chồng lịch với hợp đồng hoặc yêu cầu khác (BR-01)", !hasClash)
        ];
    }

    public static bool CanConfirm(IEnumerable<ConfirmationCheck> checks) => checks.All(c => c.Passed);
}
