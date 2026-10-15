using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class LoginViewModel(AuthService auth, Session session) : ObservableObject
{
    [ObservableProperty] private string _taiKhoan = string.Empty;
    [ObservableProperty] private string _matKhau = string.Empty;
    [ObservableProperty] private string? _thongBaoLoi;
    [ObservableProperty] private bool _dangXuLy;

    public event EventHandler? DangNhapThanhCong;

    [RelayCommand]
    private async Task DangNhapAsync()
    {
        ThongBaoLoi = null;
        if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrEmpty(MatKhau))
        {
            ThongBaoLoi = "Vui lòng nhập tài khoản và mật khẩu.";
            return;
        }

        DangXuLy = true;
        try
        {
            var nv = await auth.DangNhapAsync(TaiKhoan.Trim(), MatKhau);
            if (nv == null)
            {
                ThongBaoLoi = "Sai tài khoản hoặc mật khẩu, hoặc tài khoản đã nghỉ việc.";
                return;
            }
            session.DangNhap(TaiKhoan.Trim(), nv.MaNV, nv.TenNV, nv.ChucVu, nv.MaCa, nv.TenCa);
            MatKhau = string.Empty;
            DangNhapThanhCong?.Invoke(this, EventArgs.Empty);
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
