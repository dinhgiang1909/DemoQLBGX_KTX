using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class DoiMatKhauViewModel(AuthService auth, Session session) : ObservableObject
{
    [ObservableProperty] private string _matKhauCu = string.Empty;
    [ObservableProperty] private string _matKhauMoi = string.Empty;
    [ObservableProperty] private string _xacNhanMatKhau = string.Empty;
    [ObservableProperty] private string? _thongBaoLoi;
    [ObservableProperty] private bool _dangXuLy;

    public string TaiKhoan => session.TaiKhoan ?? string.Empty;

    /// <summary>Báo cửa sổ đóng lại sau khi đổi thành công.</summary>
    public event EventHandler? DaDoiXong;

    [RelayCommand]
    private async Task DoiMatKhauAsync()
    {
        ThongBaoLoi = null;
        if (string.IsNullOrEmpty(MatKhauCu) || string.IsNullOrEmpty(MatKhauMoi))
        {
            ThongBaoLoi = "Vui lòng nhập đủ mật khẩu cũ và mật khẩu mới.";
            return;
        }
        // Thủ tục nhận VARCHAR nên mật khẩu chỉ được gồm ký tự không dấu.
        if (MatKhauMoi.Any(c => c > 127))
        {
            ThongBaoLoi = "Mật khẩu chỉ được gồm ký tự không dấu.";
            return;
        }
        if (MatKhauMoi != XacNhanMatKhau)
        {
            ThongBaoLoi = "Xác nhận mật khẩu mới không khớp.";
            return;
        }

        DangXuLy = true;
        try
        {
            await auth.DoiMatKhauAsync(TaiKhoan, MatKhauCu, MatKhauMoi);
            DaDoiXong?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ThongBaoLoi = SqlErrorHelper.ThongDiep(ex);
        }
        finally
        {
            DangXuLy = false;
        }
    }
}
