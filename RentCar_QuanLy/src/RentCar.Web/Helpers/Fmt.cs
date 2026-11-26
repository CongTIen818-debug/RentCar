using System.Globalization;
using RentCar.Domain.Enums;

namespace RentCar.Web.Helpers;

/// <summary>Định dạng hiển thị theo chuẩn Việt Nam: tiền VND không có số lẻ, ngày dd/MM/yyyy HH:mm (mục 2.4.2).</summary>
public static class Fmt
{
    public static string Cars(RentCar.Domain.Entities.RentalContract contract) =>
        string.Join(", ", contract.Lines.Select(l => l.Vehicle.Name + " (" + l.Vehicle.PlateNumber + ")"));
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Vnd(decimal value) => value.ToString("N0", Vi) + " ₫";

    public static string When(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Vi);

    public static string ContractLabel(ContractStatus s) => s switch
    {
        ContractStatus.PendingConfirmation => "Chờ xác nhận",
        ContractStatus.Confirmed => "Đã xác nhận",
        ContractStatus.Active => "Đang thuê",
        ContractStatus.Completed => "Hoàn thành",
        ContractStatus.Cancelled => "Đã hủy",
        _ => "Không rõ"
    };

    public static string ContractCss(ContractStatus s) => s switch
    {
        ContractStatus.PendingConfirmation => "chip--warn",
        ContractStatus.Confirmed => "chip--info",
        ContractStatus.Active => "chip--active",
        ContractStatus.Completed => "chip--ok",
        ContractStatus.Cancelled => "chip--bad",
        _ => ""
    };

    public static string RequestLabel(RentalRequestStatus s) => s switch
    {
        RentalRequestStatus.Pending => "Chờ xử lý",
        RentalRequestStatus.ConvertedToContract => "Đã lập hợp đồng",
        RentalRequestStatus.Cancelled => "Đã hủy",
        _ => "Không rõ"
    };

    public static string RequestCss(RentalRequestStatus s) => s switch
    {
        RentalRequestStatus.Pending => "chip--warn",
        RentalRequestStatus.ConvertedToContract => "chip--info",
        RentalRequestStatus.Cancelled => "chip--bad",
        _ => ""
    };

    public static string PaymentLabel(PaymentType t) => t switch
    {
        PaymentType.Deposit => "Đặt cọc",
        PaymentType.Rent => "Tiền thuê",
        PaymentType.Surcharge => "Phụ phí",
        PaymentType.DepositRefund => "Hoàn cọc",
        PaymentType.Forfeit => "Giữ cọc khi hủy",
        _ => "Khác"
    };

    /// <summary>v9: nhãn tăng trưởng cho thẻ KPI, trả về (lớp CSS, nội dung).</summary>
    public static (string Css, string Text) Delta(decimal? growth)
    {
        if (growth is null) return ("kpi__delta--flat", "Mới");
        var g = growth.Value;
        return g >= 0
            ? ("kpi__delta--up", "▲ " + g.ToString("0.#", Vi) + "%")
            : ("kpi__delta--down", "▼ " + Math.Abs(g).ToString("0.#", Vi) + "%");
    }
}
