using System.IO;

namespace QLBGX.Devices;

/// <summary>Lưu ảnh chụp vào AnhBaiXe/yyyyMMdd/ cạnh file chạy; CSDL lưu đường dẫn tương đối.</summary>
public class ImageStore
{
    public const string ThuMucGoc = "AnhBaiXe";

    /// <param name="loai">"vao" hoặc "ra"</param>
    /// <returns>Đường dẫn tương đối, ví dụ AnhBaiXe/20261014/V_20261014_083015_vao.jpg</returns>
    public string Luu(string duongDanNguon, string loai)
    {
        var now = DateTime.Now;
        var thuMuc = Path.Combine(ThuMucGoc, now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, thuMuc));

        var ten = $"V_{now:yyyyMMdd_HHmmss}_{loai}";
        var tuongDoi = Path.Combine(thuMuc, ten + ".jpg");
        for (var i = 2; File.Exists(Path.Combine(AppContext.BaseDirectory, tuongDoi)); i++)
            tuongDoi = Path.Combine(thuMuc, $"{ten}_{i}.jpg");

        File.Copy(duongDanNguon, Path.Combine(AppContext.BaseDirectory, tuongDoi));
        return tuongDoi.Replace('\\', '/');
    }

    /// <summary>Đường dẫn đầy đủ của ảnh (null nếu không có tệp).</summary>
    public static string? DuongDanDayDu(string? duongDan)
    {
        if (string.IsNullOrWhiteSpace(duongDan)) return null;
        try
        {
            var full = Path.IsPathRooted(duongDan) ? duongDan : Path.Combine(AppContext.BaseDirectory, duongDan);
            return File.Exists(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }
}
