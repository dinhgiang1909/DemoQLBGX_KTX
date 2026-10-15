using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QLBGX.Devices;

namespace QLBGX.Helpers;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>null hoặc chuỗi rỗng → Collapsed.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rong = value is null || value is string { Length: 0 };
        if (parameter is "invert") rong = !rong;
        return rong ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Chuỗi khác rỗng → true (dùng cho InfoBar.IsOpen).</summary>
public class HasTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string { Length: > 0 };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool → Visibility, parameter "invert" để đảo.</summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (parameter is "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Đường dẫn ảnh (tương đối/tuyệt đối) → ImageSource; không có tệp → null. Không khóa tệp.</summary>
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var full = ImageStore.DuongDanDayDu(value as string);
        if (full == null) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri(Path.GetFullPath(full));
            bmp.DecodePixelWidth = 800;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Mức trạng thái → màu: xanh = hợp lệ, cam = cảnh báo, đỏ = lỗi/chặn, xám = ngừng/bảo trì.</summary>
public enum MucTrangThai { BinhThuong, HopLe, CanhBao, Loi, Ngung }

public class MucTrangThaiBrushConverter : IValueConverter
{
    public static readonly SolidColorBrush Xanh = Tao("#107C10");
    public static readonly SolidColorBrush Cam = Tao("#CA5010");
    public static readonly SolidColorBrush Do = Tao("#C42B1C");
    public static readonly SolidColorBrush Xam = Tao("#8A8A8A");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        MucTrangThai.HopLe => Xanh,
        MucTrangThai.CanhBao => Cam,
        MucTrangThai.Loi => Do,
        MucTrangThai.Ngung => Xam,
        _ => Brushes.Transparent,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Tao(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }
}

/// <summary>Chuỗi trạng thái trong CSDL → màu chữ thống nhất.</summary>
public class TrangThaiBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value as string) switch
    {
        "Hoạt động" or "Rảnh" or "Đang làm" or "Đã lấy" or "Đã xác nhận" or "Còn hạn" or "Vé tháng" => MucTrangThaiBrushConverter.Xanh,
        "Đang gửi" or "Đang sử dụng" or "Chờ xác nhận" or "Sắp hết hạn" or "Vé tháng (ân hạn)" => MucTrangThaiBrushConverter.Cam,
        "Khóa" or "Chờ xử lý" or "Hết hạn" or "Vé tháng quá hạn" => MucTrangThaiBrushConverter.Do,
        "Bảo trì" or "Ngừng" or "Ngừng sử dụng" or "Nghỉ việc" => MucTrangThaiBrushConverter.Xam,
        _ => DependencyProperty.UnsetValue,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Số → GridLength sao (dùng vẽ thanh tỷ lệ). parameter "rest" → 100 - giá trị.</summary>
public class PercentToStarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value switch { decimal d => (double)d, double db => db, int i => i, _ => 0 };
        v = Math.Clamp(v, 0, 100);
        if (parameter is "rest") v = 100 - v;
        return new GridLength(v, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
