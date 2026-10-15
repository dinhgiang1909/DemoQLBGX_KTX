using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;

namespace QLBGX.ViewModels;

/// <summary>Một lựa chọn có thể NULL trong ComboBox (ví dụ "Tất cả", "Bỏ ca").</summary>
public record LuaChon(string? Ma, string Ten)
{
    public override string ToString() => Ten;
}

internal static class NhapLieu
{
    public static decimal? Tien(string s) =>
        decimal.TryParse(s.Replace(".", "").Replace(",", "").Replace(" ", "").Replace("đ", ""),
            NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;

    public static TimeOnly? Gio(string s) =>
        TimeOnly.TryParseExact(s.Trim(), ["H:mm", "HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t : null;

    public static string? Rong(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// =====================================================================
// Loại xe
// =====================================================================
public partial class LoaiXeViewModel(Notifier notify, DanhMucService svc) : ViewModelBase(notify)
{
    public ObservableCollection<LoaiXe> Items { get; } = [];
    public string[] TrangThais { get; } = ["Hoạt động", "Ngừng sử dụng"];

    [ObservableProperty] private LoaiXe? _chon;
    [ObservableProperty] private bool _laThemMoi = true;
    [ObservableProperty] private string _fMa = "";
    [ObservableProperty] private string _fTen = "";
    [ObservableProperty] private string _fMoTa = "";
    [ObservableProperty] private string _fTrangThai = "Hoạt động";

    public override Task TaiAsync() => ChayAsync(TaiCoreAsync);

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var x in await svc.LayLoaiXeAsync()) Items.Add(x);
    }

    partial void OnChonChanged(LoaiXe? value)
    {
        if (value == null) return;
        LaThemMoi = false;
        FMa = value.MaLoaiXe;
        FTen = value.TenLoai;
        FMoTa = value.MoTa ?? "";
        FTrangThai = value.TrangThai;
    }

    [RelayCommand]
    private void ThemMoi()
    {
        Chon = null;
        LaThemMoi = true;
        FMa = FTen = FMoTa = "";
        FTrangThai = "Hoạt động";
    }

    [RelayCommand]
    private async Task LuuAsync()
    {
        if (string.IsNullOrWhiteSpace(FMa) || string.IsNullOrWhiteSpace(FTen))
        {
            Notify.Loi("Nhập mã và tên loại xe.");
            return;
        }
        var lx = new LoaiXe { MaLoaiXe = FMa.Trim().ToUpperInvariant(), TenLoai = FTen.Trim(), MoTa = NhapLieu.Rong(FMoTa), TrangThai = FTrangThai };
        if (await ChayAsync(() => svc.LuuLoaiXeAsync(lx, LaThemMoi), "Đã lưu loại xe."))
        {
            await ChayAsync(TaiCoreAsync);
            Chon = Items.FirstOrDefault(x => x.MaLoaiXe == lx.MaLoaiXe);
        }
    }

    [RelayCommand]
    private async Task NgungAsync()
    {
        if (Chon == null) return;
        FTrangThai = "Ngừng sử dụng";
        await LuuAsync();
    }
}

// =====================================================================
// Khu vực đỗ
// =====================================================================
public class KhuVucRow
{
    public required KhuVucDo KhuVuc { get; init; }
    public int SoXeDangGui { get; init; }
    public string MaKhuVuc => KhuVuc.MaKhuVuc;
    public string TenKhuVuc => KhuVuc.TenKhuVuc;
    public int SucChua => KhuVuc.SucChua;
    public string MaLoaiXe => KhuVuc.MaLoaiXe;
    public string TrangThai => KhuVuc.TrangThai;
    public string? GhiChu => KhuVuc.GhiChu;
}

public partial class KhuVucViewModel(Notifier notify, DanhMucService svc) : ViewModelBase(notify)
{
    public ObservableCollection<KhuVucRow> Items { get; } = [];
    public ObservableCollection<LoaiXe> LoaiXes { get; } = [];
    public string[] TrangThais { get; } = ["Hoạt động", "Bảo trì", "Ngừng sử dụng"];

    [ObservableProperty] private KhuVucRow? _chon;
    [ObservableProperty] private bool _laThemMoi = true;
    [ObservableProperty] private string _fMa = "";
    [ObservableProperty] private string _fTen = "";
    [ObservableProperty] private string _fSucChua = "";
    [ObservableProperty] private string? _fMaLoaiXe;
    [ObservableProperty] private string _fTrangThai = "Hoạt động";
    [ObservableProperty] private string _fGhiChu = "";
    [ObservableProperty] private int _fSoXeDangGui;

    public override Task TaiAsync() => ChayAsync(async () =>
    {
        LoaiXes.Clear();
        foreach (var l in await svc.LayLoaiXeAsync()) LoaiXes.Add(l);
        await TaiCoreAsync();
    });

    private async Task TaiCoreAsync()
    {
        var soXe = await svc.LaySoXeDangGuiAsync();
        Items.Clear();
        foreach (var k in await svc.LayKhuVucAsync())
            Items.Add(new KhuVucRow { KhuVuc = k, SoXeDangGui = soXe.GetValueOrDefault(k.MaKhuVuc) });
    }

    partial void OnChonChanged(KhuVucRow? value)
    {
        if (value == null) return;
        LaThemMoi = false;
        FMa = value.MaKhuVuc;
        FTen = value.TenKhuVuc;
        FSucChua = value.SucChua.ToString();
        FMaLoaiXe = value.MaLoaiXe;
        FTrangThai = value.TrangThai;
        FGhiChu = value.GhiChu ?? "";
        FSoXeDangGui = value.SoXeDangGui;
    }

    [RelayCommand]
    private void ThemMoi()
    {
        Chon = null;
        LaThemMoi = true;
        FMa = FTen = FSucChua = FGhiChu = "";
        FMaLoaiXe = null;
        FTrangThai = "Hoạt động";
        FSoXeDangGui = 0;
    }

    [RelayCommand]
    private async Task LuuAsync()
    {
        if (string.IsNullOrWhiteSpace(FMa) || string.IsNullOrWhiteSpace(FTen) || FMaLoaiXe == null)
        {
            Notify.Loi("Nhập mã, tên khu vực và chọn loại xe.");
            return;
        }
        if (!int.TryParse(FSucChua, out var sc) || sc <= 0)
        {
            Notify.Loi("Sức chứa phải là số nguyên lớn hơn 0.");
            return;
        }
        if (!LaThemMoi && sc < FSoXeDangGui)
        {
            Notify.Loi($"Không thể giảm sức chứa xuống {sc}: khu vực đang có {FSoXeDangGui} xe.");
            return;
        }
        var kv = new KhuVucDo
        {
            MaKhuVuc = FMa.Trim().ToUpperInvariant(), TenKhuVuc = FTen.Trim(), SucChua = sc, MaLoaiXe = FMaLoaiXe,
            TrangThai = FTrangThai, GhiChu = NhapLieu.Rong(FGhiChu),
        };
        if (await ChayAsync(() => svc.LuuKhuVucAsync(kv, LaThemMoi), "Đã lưu khu vực."))
        {
            await ChayAsync(TaiCoreAsync);
            Chon = Items.FirstOrDefault(x => x.MaKhuVuc == kv.MaKhuVuc);
        }
    }
}

// =====================================================================
// Bảng giá (chỉ thêm mới)
// =====================================================================
public partial class BangGiaViewModel(Notifier notify, DanhMucService svc) : ViewModelBase(notify)
{
    public ObservableCollection<BangGium> Items { get; } = [];
    public ObservableCollection<LuaChon> LocLoaiXe { get; } = [];
    public ObservableCollection<LoaiXe> LoaiXes { get; } = [];

    [ObservableProperty] private LuaChon? _locChon;
    [ObservableProperty] private string _fMa = "";
    [ObservableProperty] private string? _fMaLoaiXe;
    [ObservableProperty] private string _fGiaNgay = "";
    [ObservableProperty] private string _fGiaDem = "";
    [ObservableProperty] private string _fGiaVeThang = "";
    [ObservableProperty] private string _fPhiMatVe = "";
    [ObservableProperty] private string _fPhiCapLai = "";
    [ObservableProperty] private string _fGioDong = "23:00";
    [ObservableProperty] private string _fGioMo = "05:30";
    [ObservableProperty] private string _fGioAnHan = "09:00";
    [ObservableProperty] private DateTime? _fNgayApDung = DateTime.Today.AddDays(1);

    public override Task TaiAsync() => ChayAsync(async () =>
    {
        var lx = await svc.LayLoaiXeAsync();
        LoaiXes.Clear();
        LocLoaiXe.Clear();
        LocLoaiXe.Add(new LuaChon(null, "Tất cả loại xe"));
        foreach (var l in lx)
        {
            LoaiXes.Add(l);
            LocLoaiXe.Add(new LuaChon(l.MaLoaiXe, l.TenLoai));
        }
        // Gán trực tiếp field để không kích hoạt nạp lại danh sách hai lần
#pragma warning disable MVVMTK0034
        _locChon = LocLoaiXe[0];
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(LocChon));
        await TaiCoreAsync();
        FMa = await svc.TaoMaBangGiaMoiAsync();
    });

    partial void OnLocChonChanged(LuaChon? value) => _ = ChayAsync(TaiCoreAsync);

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var b in await svc.LayBangGiaAsync(LocChon?.Ma)) Items.Add(b);
    }

    /// <summary>Lấy giá trị của bảng giá đang áp dụng làm mẫu (giữ nguyên mã mới).</summary>
    [RelayCommand]
    private void SaoChep(BangGium? b)
    {
        if (b == null) return;
        FMaLoaiXe = b.MaLoaiXe;
        FGiaNgay = b.GiaGuiNgay.ToString("0");
        FGiaDem = b.GiaGuiDem.ToString("0");
        FGiaVeThang = b.GiaVeThang.ToString("0");
        FPhiMatVe = b.PhiMatVe.ToString("0");
        FPhiCapLai = b.PhiCapLaiThe.ToString("0");
        FGioDong = b.GioBatDauDem.ToString("HH:mm");
        FGioMo = b.GioKetThucDem.ToString("HH:mm");
        FGioAnHan = b.GioAnHanVeThang.ToString("HH:mm");
    }

    [RelayCommand]
    private async Task ThemAsync()
    {
        var ngay = NhapLieu.Tien(FGiaNgay);
        var dem = NhapLieu.Tien(FGiaDem);
        var thang = NhapLieu.Tien(FGiaVeThang);
        var mat = NhapLieu.Tien(FPhiMatVe);
        var capLai = NhapLieu.Tien(FPhiCapLai);
        var gioDong = NhapLieu.Gio(FGioDong);
        var gioMo = NhapLieu.Gio(FGioMo);
        var anHan = NhapLieu.Gio(FGioAnHan);
        if (string.IsNullOrWhiteSpace(FMa) || FMaLoaiXe == null || FNgayApDung == null)
        {
            Notify.Loi("Nhập mã bảng giá, loại xe và ngày áp dụng.");
            return;
        }
        if (ngay is not >= 0 || dem is not >= 0 || thang is not >= 0 || mat is not >= 0 || capLai is not >= 0)
        {
            Notify.Loi("Các mức giá/phí phải là số không âm.");
            return;
        }
        if (dem < ngay)
        {
            Notify.Loi("Giá gửi đêm phải lớn hơn hoặc bằng giá gửi ngày.");
            return;
        }
        if (gioDong == null || gioMo == null || anHan == null)
        {
            Notify.Loi("Giờ phải có dạng HH:mm.");
            return;
        }
        var bg = new BangGium
        {
            MaBangGia = FMa.Trim().ToUpperInvariant(), MaLoaiXe = FMaLoaiXe, GiaGuiNgay = ngay.Value, GiaGuiDem = dem.Value,
            GiaVeThang = thang.Value, PhiMatVe = mat.Value, PhiCapLaiThe = capLai.Value,
            GioBatDauDem = gioDong.Value, GioKetThucDem = gioMo.Value, GioAnHanVeThang = anHan.Value,
            NgayApDung = DateOnly.FromDateTime(FNgayApDung.Value),
        };
        if (await ChayAsync(() => svc.ThemBangGiaAsync(bg), $"Đã thêm bảng giá {bg.MaBangGia}."))
            await ChayAsync(async () =>
            {
                await TaiCoreAsync();
                FMa = await svc.TaoMaBangGiaMoiAsync();
            });
    }
}

// =====================================================================
// Khuyến mãi
// =====================================================================
public partial class KhuyenMaiViewModel(Notifier notify, DanhMucService svc) : ViewModelBase(notify)
{
    public ObservableCollection<KhuyenMai> Items { get; } = [];
    public ObservableCollection<LuaChon> LoaiXes { get; } = [];
    public LuaChon[] KhungGuis { get; } = [new(null, "Tất cả"), new("Ngày", "Ngày"), new("Đêm", "Đêm")];
    public string[] TrangThais { get; } = ["Hoạt động", "Ngừng"];

    [ObservableProperty] private KhuyenMai? _chon;
    [ObservableProperty] private bool _laThemMoi = true;
    [ObservableProperty] private string _fMa = "";
    [ObservableProperty] private string _fTen = "";
    [ObservableProperty] private string _fPhanTram = "";
    [ObservableProperty] private string _fDieuKien = "";
    [ObservableProperty] private LuaChon? _fLoaiXe;
    [ObservableProperty] private LuaChon? _fKhungGui;
    [ObservableProperty] private DateTime? _fTuNgay = DateTime.Today;
    [ObservableProperty] private DateTime? _fDenNgay = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string _fTrangThai = "Hoạt động";

    public override Task TaiAsync() => ChayAsync(async () =>
    {
        LoaiXes.Clear();
        LoaiXes.Add(new LuaChon(null, "Tất cả"));
        foreach (var l in await svc.LayLoaiXeAsync()) LoaiXes.Add(new LuaChon(l.MaLoaiXe, l.TenLoai));
        ThemMoi();
        await TaiCoreAsync();
    });

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var k in await svc.LayKhuyenMaiAsync()) Items.Add(k);
    }

    partial void OnChonChanged(KhuyenMai? value)
    {
        if (value == null) return;
        LaThemMoi = false;
        FMa = value.MaKhuyenMai;
        FTen = value.TenChuongTrinh;
        FPhanTram = value.PhanTramGiam.ToString("0.##", CultureInfo.InvariantCulture);
        FDieuKien = value.DieuKienApDung ?? "";
        FLoaiXe = LoaiXes.FirstOrDefault(l => l.Ma == value.MaLoaiXe);
        FKhungGui = KhungGuis.FirstOrDefault(k => k.Ma == value.KhungGuiApDung);
        FTuNgay = value.NgayBatDau.ToDateTime(TimeOnly.MinValue);
        FDenNgay = value.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
        FTrangThai = value.TrangThai;
    }

    [RelayCommand]
    private void ThemMoi()
    {
        Chon = null;
        LaThemMoi = true;
        FMa = FTen = FPhanTram = FDieuKien = "";
        FLoaiXe = LoaiXes.FirstOrDefault();
        FKhungGui = KhungGuis[0];
        FTuNgay = DateTime.Today;
        FDenNgay = DateTime.Today.AddMonths(1);
        FTrangThai = "Hoạt động";
    }

    [RelayCommand]
    private async Task LuuAsync()
    {
        if (string.IsNullOrWhiteSpace(FMa) || string.IsNullOrWhiteSpace(FTen) || FTuNgay == null || FDenNgay == null)
        {
            Notify.Loi("Nhập mã, tên chương trình và khoảng ngày.");
            return;
        }
        if (!decimal.TryParse(FPhanTram.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var pt)
            || pt <= 0 || pt > 100)
        {
            Notify.Loi("Tỷ lệ giảm phải trong khoảng (0, 100].");
            return;
        }
        if (FDenNgay < FTuNgay)
        {
            Notify.Loi("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            return;
        }
        var km = new KhuyenMai
        {
            MaKhuyenMai = FMa.Trim().ToUpperInvariant(), TenChuongTrinh = FTen.Trim(), PhanTramGiam = pt,
            DieuKienApDung = NhapLieu.Rong(FDieuKien), MaLoaiXe = FLoaiXe?.Ma, KhungGuiApDung = FKhungGui?.Ma,
            NgayBatDau = DateOnly.FromDateTime(FTuNgay.Value), NgayKetThuc = DateOnly.FromDateTime(FDenNgay.Value),
            TrangThai = FTrangThai,
        };
        if (await ChayAsync(() => svc.LuuKhuyenMaiAsync(km, LaThemMoi), "Đã lưu khuyến mãi."))
        {
            await ChayAsync(TaiCoreAsync);
            Chon = Items.FirstOrDefault(x => x.MaKhuyenMai == km.MaKhuyenMai);
        }
    }

    [RelayCommand]
    private async Task NgungAsync()
    {
        if (Chon == null) return;
        FTrangThai = "Ngừng";
        await LuuAsync();
    }
}

// =====================================================================
// Nhân viên
// =====================================================================
public partial class NhanVienViewModel(Notifier notify, DanhMucService svc, CaTrucService caTruc, Session session,
    DialogService dialog) : ViewModelBase(notify)
{
    public ObservableCollection<NhanVien> Items { get; } = [];
    public ObservableCollection<LuaChon> Cas { get; } = [];
    public string[] ChucVus { get; } = [Session.QuanLy, Session.ThuNgan, Session.BaoVe];

    [ObservableProperty] private NhanVien? _chon;
    [ObservableProperty] private bool _laThemMoi = true;
    [ObservableProperty] private string _fMa = "";
    [ObservableProperty] private string _fTen = "";
    [ObservableProperty] private string _fChucVu = Session.BaoVe;
    [ObservableProperty] private string _fSdt = "";
    [ObservableProperty] private DateTime? _fNgayVaoLam = DateTime.Today;
    [ObservableProperty] private string _fTaiKhoan = "";
    [ObservableProperty] private string _fMatKhau = "";
    [ObservableProperty] private LuaChon? _fCa;

    public override Task TaiAsync() => ChayAsync(async () =>
    {
        Cas.Clear();
        Cas.Add(new LuaChon(null, "(Chưa xếp ca)"));
        foreach (var c in await svc.LayCaAsync()) Cas.Add(new LuaChon(c.MaCa, $"{c.TenCa} ({c.GioBatDau:HH\\:mm}–{c.GioKetThuc:HH\\:mm})"));
        FCa = Cas[0];
        await TaiCoreAsync();
    });

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var n in await svc.LayNhanVienAsync()) Items.Add(n);
    }

    partial void OnChonChanged(NhanVien? value)
    {
        if (value == null) return;
        LaThemMoi = false;
        FMa = value.MaNv;
        FTen = value.TenNv;
        FChucVu = value.ChucVu;
        FSdt = value.Sdt ?? "";
        FNgayVaoLam = value.NgayVaoLam.ToDateTime(TimeOnly.MinValue);
        FTaiKhoan = value.TaiKhoan;
        FMatKhau = "";
        FCa = Cas.FirstOrDefault(c => c.Ma == value.MaCa) ?? Cas[0];
    }

    [RelayCommand]
    private void ThemMoi()
    {
        Chon = null;
        LaThemMoi = true;
        FMa = FTen = FSdt = FTaiKhoan = FMatKhau = "";
        FChucVu = Session.BaoVe;
        FNgayVaoLam = DateTime.Today;
        FCa = Cas.FirstOrDefault();
    }

    [RelayCommand]
    private async Task LuuAsync()
    {
        if (string.IsNullOrWhiteSpace(FMa) || string.IsNullOrWhiteSpace(FTen) || string.IsNullOrWhiteSpace(FTaiKhoan) || FNgayVaoLam == null)
        {
            Notify.Loi("Nhập mã, tên, tài khoản và ngày vào làm.");
            return;
        }
        if (LaThemMoi || FMatKhau.Length > 0)
        {
            // Thủ tục so sánh mật khẩu kiểu VARCHAR: chỉ cho ký tự không dấu, tối thiểu 6 ký tự
            if (FMatKhau.Length < 6 || FMatKhau.Any(c => c > 127))
            {
                Notify.Loi("Mật khẩu tối thiểu 6 ký tự và chỉ gồm ký tự không dấu.");
                return;
            }
        }
        var nv = new NhanVien
        {
            MaNv = FMa.Trim().ToUpperInvariant(), TenNv = FTen.Trim(), ChucVu = FChucVu, Sdt = NhapLieu.Rong(FSdt),
            NgayVaoLam = DateOnly.FromDateTime(FNgayVaoLam.Value), TaiKhoan = FTaiKhoan.Trim(), MaCa = FCa?.Ma,
        };
        var ok = LaThemMoi
            ? await ChayAsync(() => svc.ThemNhanVienAsync(nv, FMatKhau), "Đã thêm nhân viên.")
            : await ChayAsync(() => svc.SuaNhanVienAsync(nv, FMatKhau), "Đã lưu nhân viên (đổi ca dùng màn hình Cập nhật ca trực).");
        if (!ok) return;
        FMatKhau = "";
        await ChayAsync(TaiCoreAsync);
        Chon = Items.FirstOrDefault(x => x.MaNv == nv.MaNv);
    }

    /// <summary>Cho nghỉ việc = bỏ ca (sp_CapNhatCaTruc, kiểm tra hóa đơn chưa chốt) + đổi TrangThai.</summary>
    [RelayCommand]
    private async Task ChoNghiViecAsync()
    {
        if (Chon is not { TrangThai: "Đang làm" } nv) return;
        if (nv.MaNv == session.MaNV)
        {
            Notify.Loi("Không thể tự cho mình nghỉ việc.");
            return;
        }
        if (!await dialog.XacNhanAsync("Cho nghỉ việc", $"Cho {nv.TenNv} ({nv.MaNv}) nghỉ việc? Tài khoản sẽ không đăng nhập được nữa.")) return;
        var ok = await ChayAsync(async () =>
        {
            if (nv.MaCa != null) await caTruc.CapNhatCaTrucAsync(nv.MaNv, null, session.MaNV!);
            await svc.ChoNghiViecAsync(nv.MaNv);
        }, "Đã cho nghỉ việc.");
        if (ok) await ChayAsync(TaiCoreAsync);
    }
}

// =====================================================================
// Ca làm việc
// =====================================================================
public partial class CaLamViecViewModel(Notifier notify, DanhMucService svc) : ViewModelBase(notify)
{
    public ObservableCollection<CaLamViec> Items { get; } = [];

    [ObservableProperty] private CaLamViec? _chon;
    [ObservableProperty] private string _fTen = "";
    [ObservableProperty] private string _fBatDau = "";
    [ObservableProperty] private string _fKetThuc = "";

    public override Task TaiAsync() => ChayAsync(TaiCoreAsync);

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var c in await svc.LayCaAsync()) Items.Add(c);
    }

    partial void OnChonChanged(CaLamViec? value)
    {
        if (value == null) return;
        FTen = value.TenCa;
        FBatDau = value.GioBatDau.ToString("HH:mm");
        FKetThuc = value.GioKetThuc.ToString("HH:mm");
    }

    [RelayCommand]
    private async Task LuuAsync()
    {
        if (Chon == null) return;
        var bd = NhapLieu.Gio(FBatDau);
        var kt = NhapLieu.Gio(FKetThuc);
        if (string.IsNullOrWhiteSpace(FTen) || bd == null || kt == null || bd == kt)
        {
            Notify.Loi("Nhập tên ca và giờ bắt đầu/kết thúc dạng HH:mm (khác nhau).");
            return;
        }
        var ma = Chon.MaCa;
        if (await ChayAsync(() => svc.LuuCaAsync(new CaLamViec { MaCa = ma, TenCa = FTen.Trim(), GioBatDau = bd.Value, GioKetThuc = kt.Value }),
                "Đã lưu ca làm việc."))
        {
            await ChayAsync(TaiCoreAsync);
            Chon = Items.FirstOrDefault(c => c.MaCa == ma);
        }
    }
}

// =====================================================================
// Cập nhật ca trực
// =====================================================================
public partial class CaTrucViewModel(Notifier notify, DanhMucService svc, CaTrucService caTruc, Session session)
    : ViewModelBase(notify)
{
    public ObservableCollection<NhanVien> Items { get; } = [];
    public ObservableCollection<LuaChon> Cas { get; } = [];

    [ObservableProperty] private NhanVien? _chon;
    [ObservableProperty] private LuaChon? _caMoi;

    public override Task TaiAsync() => ChayAsync(async () =>
    {
        Cas.Clear();
        Cas.Add(new LuaChon(null, "Bỏ ca"));
        foreach (var c in await svc.LayCaAsync()) Cas.Add(new LuaChon(c.MaCa, $"{c.TenCa} ({c.GioBatDau:HH\\:mm}–{c.GioKetThuc:HH\\:mm})"));
        await TaiCoreAsync();
    });

    private async Task TaiCoreAsync()
    {
        Items.Clear();
        foreach (var n in (await svc.LayNhanVienAsync()).Where(n => n.TrangThai == "Đang làm")) Items.Add(n);
    }

    partial void OnChonChanged(NhanVien? value) => CaMoi = value == null ? null : Cas.FirstOrDefault(c => c.Ma == value.MaCa);

    [RelayCommand]
    private async Task CapNhatAsync()
    {
        if (Chon == null || CaMoi == null) return;
        var nv = Chon;
        var ca = CaMoi;
        if (await ChayAsync(() => caTruc.CapNhatCaTrucAsync(nv.MaNv, ca.Ma, session.MaNV!), $"Đã cập nhật ca trực cho {nv.TenNv}: {ca.Ten}."))
        {
            if (nv.MaNv == session.MaNV)
            {
                session.MaCa = ca.Ma;
                session.TenCa = ca.Ma == null ? null : ca.Ten.Split(" (")[0];
            }
            await ChayAsync(TaiCoreAsync);
            Chon = Items.FirstOrDefault(n => n.MaNv == nv.MaNv);
        }
    }
}

// =====================================================================
// Kho thẻ
// =====================================================================
public partial class KhoTheViewModel(Notifier notify, TheService svc, Session session, DialogService dialog, AppNavigator nav)
    : ViewModelBase(notify)
{
    public bool LaQuanLy => Permissions.QuanLyThe(session);
    public ObservableCollection<TheRow> Items { get; } = [];
    public LuaChon[] LoaiThes { get; } = [new(null, "Tất cả loại"), new("Thẻ lượt", "Thẻ lượt"), new("Thẻ tháng", "Thẻ tháng")];
    public LuaChon[] TrangThais { get; } = [new(null, "Tất cả trạng thái"), new("Rảnh", "Rảnh"), new("Đang sử dụng", "Đang sử dụng"), new("Khóa", "Khóa"),
        new(TheService.LocLech, "Đang sử dụng – lệch dữ liệu")];
    public string[] LoaiTheMoi { get; } = ["Thẻ lượt", "Thẻ tháng"];

    [ObservableProperty] private LuaChon? _locLoai;
    [ObservableProperty] private LuaChon? _locTrangThai;
    [ObservableProperty] private string _tuKhoa = "";
    [ObservableProperty] private TheRow? _chon;
    [ObservableProperty] private string _soLuong = "";
    [ObservableProperty] private string? _canhBaoKho;
    [ObservableProperty] private string _fMaThe = "";
    [ObservableProperty] private string _fLoaiThe = "Thẻ lượt";

    public override Task TaiAsync()
    {
#pragma warning disable MVVMTK0034
        _locLoai ??= LoaiThes[0];
        _locTrangThai ??= TrangThais[0];
        // Mở từ cảnh báo ở Tổng quan
        switch (nav.LayThamSo())
        {
            case "Rảnh":
                _locLoai = LoaiThes[1];
                _locTrangThai = TrangThais[1];
                break;
            case TheService.LocLech:
                _locLoai = LoaiThes[0];
                _locTrangThai = TrangThais[^1];
                break;
        }
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(LocLoai));
        OnPropertyChanged(nameof(LocTrangThai));
        return ChayAsync(TaiCoreAsync);
    }

    partial void OnLocLoaiChanged(LuaChon? value) => _ = ChayAsync(TaiCoreAsync);
    partial void OnLocTrangThaiChanged(LuaChon? value) => _ = ChayAsync(TaiCoreAsync);

    private async Task TaiCoreAsync()
    {
        var ds = await svc.LayDanhSachAsync(LocLoai?.Ma, LocTrangThai?.Ma, TuKhoa);
        Items.Clear();
        foreach (var t in ds) Items.Add(t);
        SoLuong = $"{ds.Count} thẻ – Rảnh {ds.Count(t => t.TrangThai == "Rảnh")}, đang sử dụng {ds.Count(t => t.TrangThai == "Đang sử dụng")}, khóa {ds.Count(t => t.TrangThai == "Khóa")}";
        var (ranh, lech) = await svc.DemCanhBaoAsync();
        var canhBao = new List<string>();
        if (ranh == 0) canhBao.Add("HẾT thẻ lượt \"Rảnh\": khai báo thêm thẻ hoặc thu hồi các thẻ thất lạc.");
        if (lech > 0) canhBao.Add($"{lech} thẻ \"Đang sử dụng\" nhưng không có lượt gửi/vé tháng (dữ liệu lệch): kiểm tra, khóa thẻ rồi mở khóa để đưa về Rảnh.");
        CanhBaoKho = canhBao.Count == 0 ? null : string.Join(Environment.NewLine, canhBao);
    }

    [RelayCommand]
    private Task TimAsync() => ChayAsync(TaiCoreAsync);

    [RelayCommand]
    private async Task KhaiBaoAsync()
    {
        if (!LaQuanLy) return;
        var ma = FMaThe.Trim().ToUpperInvariant();
        if (ma.Length == 0)
        {
            Notify.Loi("Nhập mã thẻ (quẹt thẻ mới).");
            return;
        }
        if (await ChayAsync(() => svc.KhaiBaoAsync(ma, FLoaiThe, session.MaNV!), $"Đã khai báo {FLoaiThe} {ma}."))
        {
            FMaThe = "";
            await ChayAsync(TaiCoreAsync);
        }
    }

    [RelayCommand]
    private async Task KhoaAsync()
    {
        if (!LaQuanLy || Chon == null) return;
        var ma = Chon.MaThe;
        var lyDo = await dialog.NhapAsync("Khóa thẻ", $"Lý do khóa thẻ {ma} (hỏng, thất lạc, dữ liệu lệch...)");
        if (lyDo == null) return;
        if (await ChayAsync(() => svc.KhoaAsync(ma, session.MaNV!, lyDo), $"Đã khóa thẻ {ma}."))
            await ChayAsync(TaiCoreAsync);
    }

    [RelayCommand]
    private async Task MoKhoaAsync()
    {
        if (!LaQuanLy || Chon == null) return;
        var ma = Chon.MaThe;
        if (!await dialog.XacNhanAsync("Mở khóa thẻ", $"Mở khóa thẻ {ma} (đã tìm lại được) để dùng lại?")) return;
        if (await ChayAsync(() => svc.MoKhoaAsync(ma, session.MaNV!), $"Đã mở khóa thẻ {ma}."))
            await ChayAsync(TaiCoreAsync);
    }
}
