using System.Diagnostics;
using System.IO;
using System.Text;
using QLBGX.Helpers;

namespace QLBGX.Devices;

/// <summary>
/// Gọi chương trình ANPR bên ngoài (appsettings: Anpr:ExePath, Anpr:TimeoutMs) với đường dẫn ảnh làm tham số,
/// đọc stdout (UTF-8) để lấy biển số. Lỗi / quá thời gian / không đọc được → trả rỗng và ghi log, không làm dừng app.
/// </summary>
public class ExeBienSoReader(string exePath, int timeoutMs) : IBienSoReader
{
    public async Task<string> DocBienSoAsync(string duongDanAnh)
    {
        try
        {
            var exe = Path.IsPathRooted(exePath) ? exePath : Path.Combine(AppContext.BaseDirectory, exePath);
            if (!File.Exists(exe))
            {
                Log.Ghi($"ANPR: không tìm thấy {exe}.");
                return string.Empty;
            }

            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            psi.ArgumentList.Add(duongDanAnh);

            using var p = Process.Start(psi);
            if (p == null) return string.Empty;
            using var cts = new CancellationTokenSource(timeoutMs);
            var docOut = p.StandardOutput.ReadToEndAsync(cts.Token);
            try
            {
                await p.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(true); } catch { /* bỏ qua */ }
                Log.Ghi($"ANPR: quá thời gian {timeoutMs} ms.");
                return string.Empty;
            }

            var output = await docOut;
            foreach (var dong in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (BienSoHelper.CoDangBienSo(dong))
                    return BienSoHelper.ChuanHoa(dong);

            Log.Ghi($"ANPR: không đọc được biển số. stdout = {output.Trim()}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Ghi("ANPR lỗi: " + ex.Message);
            return string.Empty;
        }
    }
}
