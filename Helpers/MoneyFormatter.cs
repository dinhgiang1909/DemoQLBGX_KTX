using System.Globalization;
using System.Windows.Data;

namespace QLBGX.Helpers;

public static class MoneyFormatter
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(decimal? soTien) => soTien.HasValue ? soTien.Value.ToString("#,##0", Vi) + " đ" : string.Empty;

    public static string NgayGio(DateTime? d) => d?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;

    public static string Ngay(DateOnly? d) => d?.ToString("dd/MM/yyyy") ?? string.Empty;
}

/// <summary>Converter XAML: số tiền → "#,##0 đ".</summary>
public class MoneyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            decimal d => MoneyFormatter.Format(d),
            int i => MoneyFormatter.Format(i),
            long l => MoneyFormatter.Format(l),
            double db => MoneyFormatter.Format((decimal)db),
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
