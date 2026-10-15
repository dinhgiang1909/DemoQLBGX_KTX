using System.Text.RegularExpressions;

namespace QLBGX.Helpers;

/// <summary>
/// Chuẩn hóa biển số giống dbo.fn_ChuanHoaBienSo. Chỉ dùng để hiển thị và so sánh trên giao diện;
/// CSDL tự chuẩn hóa lại khi lưu.
/// </summary>
public static partial class BienSoHelper
{
    public static string ChuanHoa(string? bienSo)
    {
        if (string.IsNullOrWhiteSpace(bienSo)) return string.Empty;
        var s = bienSo.Trim().ToUpperInvariant();
        foreach (var c in new[] { " ", ".", "-", "_", "/" })
            s = s.Replace(c, string.Empty);
        return s;
    }

    public static bool GiongNhau(string? a, string? b) => ChuanHoa(a) == ChuanHoa(b);

    /// <summary>"59X112345" → "59X1-123.45"; "59X11234" → "59X1-1234". Không khớp mẫu thì trả nguyên dạng chuẩn hóa.</summary>
    public static string HienThi(string? bienSo)
    {
        var s = ChuanHoa(bienSo);
        var m = MauBienSo().Match(s);
        if (!m.Success) return s;
        var dau = m.Groups[1].Value;
        var so = m.Groups[2].Value;
        return so.Length == 5 ? $"{dau}-{so[..3]}.{so[3..]}" : $"{dau}-{so}";
    }

    /// <summary>Có phải chuỗi có dạng biển số (dùng khi đọc từ tên tệp ảnh).</summary>
    public static bool CoDangBienSo(string? s) => MauBienSo().IsMatch(ChuanHoa(s));

    [GeneratedRegex(@"^(\d{2}[A-Z]{1,2}\d?)(\d{4,5})$")]
    private static partial Regex MauBienSo();
}
