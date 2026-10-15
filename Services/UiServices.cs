using System.Windows;
using System.Windows.Controls;
using QLBGX.Helpers;
using Wpf.Ui;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace QLBGX.Services;

/// <summary>Thông báo dạng Snackbar (WPF-UI) thay cho MessageBox.</summary>
public class Notifier(ISnackbarService snackbar)
{
    public void ThanhCong(string msg) =>
        Hien("Thành công", msg, ControlAppearance.Success, SymbolRegular.CheckmarkCircle24, 4);

    public void ThongTin(string msg) =>
        Hien("Thông báo", msg, ControlAppearance.Info, SymbolRegular.Info24, 4);

    public void CanhBao(string msg) =>
        Hien("Cảnh báo", msg, ControlAppearance.Caution, SymbolRegular.Warning24, 6);

    public void Loi(string msg) =>
        Hien("Lỗi", msg, ControlAppearance.Danger, SymbolRegular.ErrorCircle24, 8);

    public void Loi(Exception ex)
    {
        if (!SqlErrorHelper.LaLoiNghiepVu(ex)) Log.Ghi(ex.ToString());
        Loi(SqlErrorHelper.ThongDiep(ex));
    }

    private void Hien(string title, string msg, ControlAppearance ap, SymbolRegular icon, int giay)
    {
        if (snackbar.GetSnackbarPresenter() == null) return;
        snackbar.Show(title, msg, ap, new SymbolIcon(icon), TimeSpan.FromSeconds(giay));
    }
}

/// <summary>Hộp thoại nhập liệu / xác nhận dùng ContentDialog của WPF-UI.</summary>
public class DialogService(IContentDialogService dialogs)
{
    public async Task<bool> XacNhanAsync(string tieuDe, string noiDung, string nutDongY = "Đồng ý")
    {
        var dlg = new ContentDialog
        {
            Title = tieuDe,
            Content = new TextBlock { Text = noiDung, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 },
            PrimaryButtonText = nutDongY,
            CloseButtonText = "Hủy",
        };
        return await dialogs.ShowAsync(dlg, CancellationToken.None) == ContentDialogResult.Primary;
    }

    /// <returns>null nếu bấm Hủy; ngược lại chuỗi đã Trim (có thể rỗng nếu không bắt buộc).</returns>
    public async Task<string?> NhapAsync(string tieuDe, string nhan, bool batBuoc = true, string? macDinh = null)
    {
        while (true)
        {
            var tb = new TextBox { Text = macDinh ?? string.Empty, MinWidth = 380, PlaceholderText = batBuoc ? "Bắt buộc nhập" : "Có thể để trống" };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = nhan, Margin = new Thickness(0, 0, 0, 6) });
            panel.Children.Add(tb);
            var dlg = new ContentDialog
            {
                Title = tieuDe,
                Content = panel,
                PrimaryButtonText = "Đồng ý",
                CloseButtonText = "Hủy",
            };
            dlg.Loaded += (_, _) => tb.Focus();
            if (await dialogs.ShowAsync(dlg, CancellationToken.None) != ContentDialogResult.Primary) return null;
            var kq = tb.Text.Trim();
            if (!batBuoc || kq.Length > 0) return kq;
            macDinh = kq;
        }
    }
}

/// <summary>Điều hướng giữa các trang (ví dụ bấm cảnh báo ở Dashboard → mở màn hình tương ứng).</summary>
public class AppNavigator
{
    private NavigationView? _nav;
    private string? _thamSo;

    public void Gan(NavigationView nav) => _nav = nav;

    public void DenTrang(Type trang, string? thamSo = null)
    {
        _thamSo = thamSo;
        _nav?.Navigate(trang);
    }

    /// <summary>Lấy tham số điều hướng một lần (đọc xong thì xóa).</summary>
    public string? LayThamSo()
    {
        var t = _thamSo;
        _thamSo = null;
        return t;
    }
}
