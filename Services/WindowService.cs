using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QLBGX.Helpers;
using QLBGX.Views;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace QLBGX.Services;

/// <summary>Mở/đóng các cửa sổ: Đăng nhập → Cửa sổ chính → Đăng xuất về Đăng nhập.</summary>
public class WindowService(IServiceProvider sp, Session session)
{
    private MainWindow? _main;
    private bool _dangDangXuat;

    public void MoDangNhap()
    {
        var login = sp.GetRequiredService<LoginWindow>();
        var daDangNhap = false;
        login.ViewModel.DangNhapThanhCong += (_, _) =>
        {
            try
            {
                MoCuaSoChinh();
            }
            catch
            {
                session.DangXuat();
                throw;   // LoginViewModel hiển thị lỗi
            }
            daDangNhap = true;
            login.Close();
        };
        login.Closed += (_, _) =>
        {
            if (!daDangNhap) Application.Current.Shutdown();
        };
        Application.Current.MainWindow = login;
        login.Show();
    }

    private void MoCuaSoChinh()
    {
        _dangDangXuat = false;
        _main = sp.GetRequiredService<MainWindow>();
        _main.Closed += (_, _) =>
        {
            _main = null;
            if (_dangDangXuat)
            {
                session.DangXuat();
                MoDangNhap();
            }
            else
            {
                Application.Current.Shutdown();
            }
        };
        Application.Current.MainWindow = _main;
        _main.Show();
    }

    public void DangXuat()
    {
        if (_main == null) return;
        _dangDangXuat = true;
        _main.Close();
    }

    public void MoDoiMatKhau()
    {
        var dlg = sp.GetRequiredService<DoiMatKhauWindow>();
        dlg.Owner = _main;
        if (dlg.ShowDialog() == true)
        {
            sp.GetRequiredService<ISnackbarService>().Show(
                "Đổi mật khẩu", "Đã đổi mật khẩu thành công.", ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(4));
        }
    }
}
