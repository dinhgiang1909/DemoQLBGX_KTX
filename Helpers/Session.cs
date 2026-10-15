using CommunityToolkit.Mvvm.ComponentModel;

namespace QLBGX.Helpers;

/// <summary>Nhân viên đang đăng nhập (singleton qua DI).</summary>
public partial class Session : ObservableObject
{
    public const string QuanLy = "Quản lý";
    public const string ThuNgan = "Thu ngân";
    public const string BaoVe = "Bảo vệ";

    [ObservableProperty] private string? _maNV;
    [ObservableProperty] private string? _tenNV;
    [ObservableProperty] private string? _chucVu;
    [ObservableProperty] private string? _maCa;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TenCaHienThi))] private string? _tenCa;
    [ObservableProperty] private string? _taiKhoan;

    public bool DaDangNhap => !string.IsNullOrEmpty(MaNV);
    public bool LaQuanLy => ChucVu == QuanLy;
    public bool LaThuNgan => ChucVu == ThuNgan;
    public bool LaBaoVe => ChucVu == BaoVe;

    public string TenCaHienThi => string.IsNullOrEmpty(TenCa) ? "Chưa phân ca" : TenCa!;

    public void DangNhap(string taiKhoan, string maNV, string tenNV, string chucVu, string? maCa, string? tenCa)
    {
        TaiKhoan = taiKhoan;
        MaNV = maNV;
        TenNV = tenNV;
        ChucVu = chucVu;
        MaCa = maCa;
        TenCa = tenCa;
    }

    public void DangXuat()
    {
        TaiKhoan = MaNV = TenNV = ChucVu = MaCa = TenCa = null;
    }
}
