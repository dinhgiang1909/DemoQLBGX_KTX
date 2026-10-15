using System.Diagnostics;
using System.IO;

namespace QLBGX.Helpers;

/// <summary>Ghi log đơn giản ra logs/app.log cạnh file chạy.</summary>
public static class Log
{
    private static readonly object Khoa = new();

    public static void Ghi(string thongDiep)
    {
        var dong = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {thongDiep}";
        Debug.WriteLine(dong);
        try
        {
            var thuMuc = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(thuMuc);
            lock (Khoa) File.AppendAllText(Path.Combine(thuMuc, "app.log"), dong + Environment.NewLine);
        }
        catch
        {
            // không để lỗi ghi log làm dừng app
        }
    }
}
