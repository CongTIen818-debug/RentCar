using System.Globalization;
using System.Text;

namespace RentCar.Web.Helpers;

/// <summary>Dựng đường cong mượt cho biểu đồ SVG (Catmull-Rom chuyển thành Bezier). Không dùng thư viện ngoài.</summary>
public static class ChartSvg
{
    public static (string Line, string Area) Build(IReadOnlyList<decimal> values, double width, double height, double pad = 12)
    {
        if (values.Count == 0) return ("", "");

        var max = values.Max();
        var range = max <= 0 ? 1m : max;
        var points = new List<(double X, double Y)>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var x = values.Count == 1 ? width / 2 : pad + i * (width - 2 * pad) / (values.Count - 1);
            var y = height - pad - (double)(values[i] / range) * (height - 2 * pad);
            points.Add((x, y));
        }

        var sb = new StringBuilder();
        sb.Append('M').Append(Fmt(points[0].X)).Append(',').Append(Fmt(points[0].Y));
        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(points.Count - 1, i + 2)];
            var c1x = p1.X + (p2.X - p0.X) / 6;
            var c1y = p1.Y + (p2.Y - p0.Y) / 6;
            var c2x = p2.X - (p3.X - p1.X) / 6;
            var c2y = p2.Y - (p3.Y - p1.Y) / 6;
            sb.Append(" C").Append(Fmt(c1x)).Append(',').Append(Fmt(c1y))
              .Append(' ').Append(Fmt(c2x)).Append(',').Append(Fmt(c2y))
              .Append(' ').Append(Fmt(p2.X)).Append(',').Append(Fmt(p2.Y));
        }

        var line = sb.ToString();
        var baseline = Fmt(height - pad);
        var area = line + " L" + Fmt(points[^1].X) + "," + baseline + " L" + Fmt(points[0].X) + "," + baseline + " Z";
        return (line, area);
    }

    private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
