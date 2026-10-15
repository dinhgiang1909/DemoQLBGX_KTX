using System.Windows;
using System.Windows.Controls;
using QLBGX.Models;
using QLBGX.Views;

namespace QLBGX.Services;

/// <summary>Mở cửa sổ hóa đơn / biên bản chốt ca và in bằng PrintDialog.</summary>
public class PrintService(HoaDonService hoaDon, Notifier notify)
{
    public async Task MoHoaDonAsync(long maHoaDon)
    {
        try
        {
            var dto = await hoaDon.LayHoaDonInAsync(maHoaDon);
            if (dto == null)
            {
                notify.Loi($"Không tìm thấy hóa đơn #{maHoaDon}.");
                return;
            }
            new HoaDonWindow(dto) { Owner = Application.Current.MainWindow }.ShowDialog();
        }
        catch (Exception ex)
        {
            notify.Loi(ex);
        }
    }

    public void MoBienBanChotCa(LichSuCaTruc phien) =>
        new BienBanChotCaWindow(phien) { Owner = Application.Current.MainWindow }.ShowDialog();

    public static void InVisual(FrameworkElement noiDung, string moTa)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintVisual(noiDung, moTa);
    }
}
