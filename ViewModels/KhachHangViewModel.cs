using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Devices;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class KhachHangViewModel : ViewModelBase
{
    private readonly VeThangService _svc;
    private readonly DanhMucService _danhMuc;
    private readonly ICardReader _card;
    private readonly Session _session;
    private readonly DialogService _dialog;
    private readonly PrintService _print;

    public KhachHangViewModel(Notifier notify, VeThangService svc, DanhMucService danhMuc, ICardReader card,
        Session session, DialogService dialog, PrintService print) : base(notify)
    {
        _svc = svc;
        _danhMuc = danhMuc;
        _card = card;
        _session = session;
        _dialog = dialog;
        _print = print;
    }

    public bool CoTheSuaKhachHang => Permissions.SuaKhachHang(_session);
    public bool CoTheNghiepVu => Permissions.NghiepVuVeThang(_session);
    public bool CoTheMoKhoa => Permissions.MoKhoaVeThang(_session);

    public string[] DoiTuongs { get; } = ["Sinh viên KTX", "Cán bộ KTX"];
    public string[] PhuongThucs { get; } = ["Tiền mặt", "Chuyển khoản"];
    public int[] SoThangs { get; } = Enumerable.Range(1, 12).ToArray();

    public ObservableCollection<KhachHang> KhachHangs { get; } = [];
    public ObservableCollection<VeThangRow> VeThangs { get; } = [];
    public ObservableCollection<LoaiXe> LoaiXes { get; } = [];
    public ObservableCollection<GoiYThe> TheThangRanh { get; } = [];
    public ObservableCollection<LuotGuiLichSu> LsLuotGui { get; } = [];
    public ObservableCollection<HoaDonLichSu> LsHoaDon { get; } = [];

    [ObservableProperty] private string _tuKhoa = "";
    [ObservableProperty] private KhachHang? _khachHangChon;

    // Form khách hàng
    [ObservableProperty] private int _fMaKh;
    [ObservableProperty] private string _fTenKh = "";
    [ObservableProperty] private string _fMaDinhDanh = "";
    [ObservableProperty] private string _fDoiTuong = "Sinh viên KTX";
    [ObservableProperty] private string _fDonVi = "";
    [ObservableProperty] private string _fSdt = "";
    [ObservableProperty] private string _fEmail = "";
    [ObservableProperty] private string _fTrangThai = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoVeChon), nameof(GhNgayHetHanMoi), nameof(GhCachTinh), nameof(GhSoTien))]
    private VeThangRow? _veThangChon;

    // Đăng ký
    [ObservableProperty] private string _dkBienSo = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DkSoTien))] private LoaiXe? _dkLoaiXe;
    [ObservableProperty] private string _dkMauSac = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DkSoTien), nameof(DkNgayHetHan))] private int _dkSoThang = 1;
    [ObservableProperty] private string _dkMaThe = "";
    [ObservableProperty] private string _dkPhuongThuc = "Tiền mặt";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DkSoTien))] private BangGium? _dkBangGia;

    // Gia hạn
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(GhNgayHetHanMoi), nameof(GhSoTien))] private int _ghSoThang = 1;
    [ObservableProperty] private string _ghPhuongThuc = "Tiền mặt";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(GhSoTien), nameof(ClPhi))] private BangGium? _bangGiaVeChon;

    // Cấp lại thẻ
    [ObservableProperty] private string _clMaTheMoi = "";
    [ObservableProperty] private string _clPhuongThuc = "Tiền mặt";

    // Lịch sử
    [ObservableProperty] private DateTime? _lsTu = DateTime.Today.AddMonths(-3);
    [ObservableProperty] private DateTime? _lsDen = DateTime.Today;
    [ObservableProperty] private HoaDonLichSu? _lsHoaDonChon;

    public bool CoVeChon => VeThangChon != null;
    private static DateOnly HomNay => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Số tiền dự kiến = số tháng × GiaVeThang của bảng giá hiện hành.</summary>
    public string DkSoTien => DkBangGia == null ? "(chưa có bảng giá)" : MoneyFormatter.Format(DkSoThang * DkBangGia.GiaVeThang);
    public string DkNgayHetHan => HomNay.AddMonths(DkSoThang).AddDays(-1).ToString("dd/MM/yyyy");

    /// <summary>QT11: nối tiếp nếu NgayHetHan cũ ≥ hôm nay − 7 ngày, ngược lại tính từ hôm nay.</summary>
    private DateOnly? GhBatDau => VeThangChon == null ? null
        : VeThangChon.NgayHetHan >= HomNay.AddDays(-7) ? VeThangChon.NgayHetHan.AddDays(1) : HomNay;

    public string GhNgayHetHanMoi => GhBatDau is { } bd ? bd.AddMonths(GhSoThang).AddDays(-1).ToString("dd/MM/yyyy") : "";
    public string GhCachTinh => VeThangChon == null ? ""
        : VeThangChon.NgayHetHan >= HomNay.AddDays(-7)
            ? $"Nối tiếp từ {VeThangChon.NgayHetHan.AddDays(1):dd/MM/yyyy} (hạn cũ {VeThangChon.NgayHetHan:dd/MM/yyyy})"
            : $"Tính từ hôm nay {HomNay:dd/MM/yyyy} (hạn cũ đã quá 7 ngày)";
    public string GhSoTien => BangGiaVeChon == null ? "" : MoneyFormatter.Format(GhSoThang * BangGiaVeChon.GiaVeThang);
    public string ClPhi => BangGiaVeChon == null ? "" : MoneyFormatter.Format(BangGiaVeChon.PhiCapLaiThe);

    public override async Task TaiAsync()
    {
        await ChayAsync(async () =>
        {
            LoaiXes.Clear();
            foreach (var l in await _danhMuc.LayLoaiXeAsync(true)) LoaiXes.Add(l);
            await TaiTheRanhAsync();
            await TimCoreAsync();
        });
    }

    private async Task TaiTheRanhAsync()
    {
        TheThangRanh.Clear();
        foreach (var t in await _card.LayGoiYAsync(NguCanhQuetThe.TheThangRanh)) TheThangRanh.Add(t);
    }

    private async Task TimCoreAsync()
    {
        var giuMa = KhachHangChon?.MaKh;
        var ds = await _svc.TimKhachHangAsync(TuKhoa);
        KhachHangs.Clear();
        foreach (var k in ds) KhachHangs.Add(k);

        // Từ khóa là mã thẻ tháng hoặc biển số → chọn sẵn khách và vé tháng đó (gia hạn theo thẻ/biển số)
        var ve = string.IsNullOrWhiteSpace(TuKhoa) ? null : await _svc.TimVeThangTheoTheHoacBienSoAsync(TuKhoa);
        if (ve is { } v)
        {
            _maVeUuTien = v.MaThanhVien;
            var kh = KhachHangs.FirstOrDefault(k => k.MaKh == v.MaKh);
            if (kh != null && kh == KhachHangChon) await TaiVeThangAsync();
            else KhachHangChon = kh;
            return;
        }
        if (giuMa != null) KhachHangChon = KhachHangs.FirstOrDefault(k => k.MaKh == giuMa);
    }

    /// <summary>Vé tháng cần chọn sau khi tải danh sách vé (khi tìm theo thẻ/biển số).</summary>
    private int? _maVeUuTien;

    [RelayCommand]
    private Task TimAsync() => ChayAsync(TimCoreAsync);

    partial void OnKhachHangChonChanged(KhachHang? value)
    {
        if (value != null)
        {
            FMaKh = value.MaKh;
            FTenKh = value.TenKh;
            FMaDinhDanh = value.MaDinhDanh;
            FDoiTuong = value.DoiTuong;
            FDonVi = value.DonVi ?? "";
            FSdt = value.Sdt ?? "";
            FEmail = value.Email ?? "";
            FTrangThai = value.TrangThai;
        }
        _ = TaiVeThangAsync();
    }

    private async Task TaiVeThangAsync()
    {
        VeThangs.Clear();
        LsLuotGui.Clear();
        LsHoaDon.Clear();
        if (KhachHangChon == null) return;
        try
        {
            var uuTien = _maVeUuTien;
            _maVeUuTien = null;
            foreach (var v in await _svc.LayVeThangAsync(KhachHangChon.MaKh)) VeThangs.Add(v);
            VeThangChon = VeThangs.FirstOrDefault(v => v.MaThanhVien == uuTien) ?? VeThangs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }

    partial void OnVeThangChonChanged(VeThangRow? value)
    {
        LsLuotGui.Clear();
        LsHoaDon.Clear();
        _ = TaiBangGiaVeAsync();
    }

    private async Task TaiBangGiaVeAsync()
    {
        try
        {
            BangGiaVeChon = VeThangChon == null ? null : await _svc.LayBangGiaHienHanhAsync(VeThangChon.MaLoaiXe);
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }

    partial void OnDkLoaiXeChanged(LoaiXe? value) => _ = TaiBangGiaDkAsync();

    private async Task TaiBangGiaDkAsync()
    {
        try
        {
            DkBangGia = DkLoaiXe == null ? null : await _svc.LayBangGiaHienHanhAsync(DkLoaiXe.MaLoaiXe);
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }

    // ---------- Khách hàng ----------
    [RelayCommand]
    private void ThemMoiKhachHang()
    {
        KhachHangChon = null;
        FMaKh = 0;
        FTenKh = FMaDinhDanh = FDonVi = FSdt = FEmail = "";
        FDoiTuong = "Sinh viên KTX";
        FTrangThai = "";
    }

    [RelayCommand]
    private async Task LuuKhachHangAsync()
    {
        if (!CoTheSuaKhachHang) return;
        if (string.IsNullOrWhiteSpace(FTenKh) || string.IsNullOrWhiteSpace(FMaDinhDanh))
        {
            Notify.Loi("Nhập tên khách hàng và MSSV/mã cán bộ.");
            return;
        }
        KhachHang? luu = null;
        var ok = await ChayAsync(async () =>
        {
            luu = await _svc.LuuKhachHangAsync(new KhachHang
            {
                MaKh = FMaKh,
                TenKh = FTenKh.Trim(),
                MaDinhDanh = FMaDinhDanh.Trim(),
                DoiTuong = FDoiTuong,
                DonVi = string.IsNullOrWhiteSpace(FDonVi) ? null : FDonVi.Trim(),
                Sdt = string.IsNullOrWhiteSpace(FSdt) ? null : FSdt.Trim(),
                Email = string.IsNullOrWhiteSpace(FEmail) ? null : FEmail.Trim(),
            });
        }, "Đã lưu khách hàng.");
        if (ok && luu != null)
        {
            TuKhoa = "";
            await TimCoreAsync();
            KhachHangChon = KhachHangs.FirstOrDefault(k => k.MaKh == luu.MaKh);
        }
    }

    [RelayCommand]
    private async Task DoiTrangThaiKhachHangAsync()
    {
        if (!CoTheSuaKhachHang || KhachHangChon == null) return;
        var moi = KhachHangChon.TrangThai == "Hoạt động" ? "Ngừng" : "Hoạt động";
        if (!await _dialog.XacNhanAsync("Đổi trạng thái khách hàng", $"Chuyển {KhachHangChon.TenKh} sang \"{moi}\"?")) return;
        if (await ChayAsync(() => _svc.DoiTrangThaiKhachHangAsync(KhachHangChon.MaKh, moi), $"Đã chuyển sang \"{moi}\"."))
            await ChayAsync(TimCoreAsync);
    }

    // ---------- Đăng ký vé tháng ----------
    [RelayCommand]
    private async Task DangKyAsync()
    {
        if (!CoTheNghiepVu) return;
        if (KhachHangChon == null)
        {
            Notify.Loi("Chọn khách hàng trước.");
            return;
        }
        if (string.IsNullOrWhiteSpace(DkBienSo) || DkLoaiXe == null || string.IsNullOrWhiteSpace(DkMaThe))
        {
            Notify.Loi("Nhập biển số, loại xe và quẹt thẻ tháng đang Rảnh.");
            return;
        }
        var maThe = _card.ChuanHoaMaThe(DkMaThe);
        if (!await _dialog.XacNhanAsync("Xác nhận đăng ký vé tháng",
                $"Khách: {KhachHangChon.TenKh}\nXe: {BienSoHelper.HienThi(DkBienSo)} ({DkLoaiXe.TenLoai})\nThẻ: {maThe}\n" +
                $"Số tháng: {DkSoThang} – hết hạn dự kiến {DkNgayHetHan}\nSố tiền: {DkSoTien} – {DkPhuongThuc}")) return;

        long maHd = 0;
        var ok = await ChayAsync(async () =>
        {
            var kq = await _svc.DangKyAsync(KhachHangChon.MaKh, DkBienSo, DkLoaiXe.MaLoaiXe, maThe, DkSoThang,
                _session.MaNV!, DkMauSac, DkPhuongThuc);
            maHd = kq.MaHoaDon;
        }, "Đã đăng ký vé tháng.");
        if (!ok) return;
        DkBienSo = DkMauSac = DkMaThe = "";
        DkSoThang = 1;
        await ChayAsync(TaiTheRanhAsync);
        await TaiVeThangAsync();
        await _print.MoHoaDonAsync(maHd);
    }

    // ---------- Gia hạn ----------
    [RelayCommand]
    private async Task GiaHanAsync()
    {
        if (!CoTheNghiepVu || VeThangChon == null) return;
        var ve = VeThangChon;
        if (!await _dialog.XacNhanAsync("Xác nhận gia hạn vé tháng",
                $"Xe {ve.BienSo} – thẻ {ve.MaThe}\n{GhCachTinh}\nGia hạn {GhSoThang} tháng → hết hạn mới {GhNgayHetHanMoi}\n" +
                $"Số tiền: {GhSoTien} – {GhPhuongThuc}")) return;
        long maHd = 0;
        var ok = await ChayAsync(async () => maHd = await _svc.GiaHanAsync(ve.MaThanhVien, GhSoThang, _session.MaNV!, GhPhuongThuc),
            "Đã gia hạn vé tháng.");
        if (!ok) return;
        await TaiVeThangAsync();
        VeThangChon = VeThangs.FirstOrDefault(v => v.MaThanhVien == ve.MaThanhVien);
        await _print.MoHoaDonAsync(maHd);
    }

    // ---------- Khóa / mở khóa ----------
    [RelayCommand]
    private async Task KhoaAsync()
    {
        if (!CoTheNghiepVu || VeThangChon == null) return;
        var ve = VeThangChon;
        var lyDo = await _dialog.NhapAsync("Khóa vé tháng", $"Lý do khóa vé tháng xe {ve.BienSo} (thẻ {ve.MaThe} cũng bị khóa)");
        if (lyDo == null) return;
        if (await ChayAsync(() => _svc.KhoaAsync(ve.MaThanhVien, _session.MaNV!, lyDo), "Đã khóa vé tháng."))
            await TaiVeThangAsync();
    }

    [RelayCommand]
    private async Task MoKhoaAsync()
    {
        if (!CoTheMoKhoa || VeThangChon == null) return;
        var ve = VeThangChon;
        if (!await _dialog.XacNhanAsync("Mở khóa vé tháng", $"Mở khóa vé tháng xe {ve.BienSo}, thẻ {ve.MaThe} được dùng lại?")) return;
        if (await ChayAsync(() => _svc.MoKhoaAsync(ve.MaThanhVien, _session.MaNV!), "Đã mở khóa vé tháng."))
            await TaiVeThangAsync();
    }

    // ---------- Cấp lại thẻ ----------
    [RelayCommand]
    private async Task CapLaiTheAsync()
    {
        if (!CoTheNghiepVu || VeThangChon == null) return;
        var ve = VeThangChon;
        var maMoi = _card.ChuanHoaMaThe(ClMaTheMoi);
        if (maMoi.Length == 0)
        {
            Notify.Loi("Quẹt thẻ tháng mới đang Rảnh.");
            return;
        }
        if (!await _dialog.XacNhanAsync("Cấp lại thẻ tháng",
                $"Xe {ve.BienSo}: khóa thẻ cũ {ve.MaThe}, cấp thẻ mới {maMoi}.\nPhí cấp lại: {ClPhi} – {ClPhuongThuc}")) return;
        long maHd = 0;
        var ok = await ChayAsync(async () => maHd = await _svc.CapLaiTheAsync(ve.MaThanhVien, maMoi, _session.MaNV!, ClPhuongThuc),
            $"Đã cấp lại thẻ {maMoi}, thẻ cũ {ve.MaThe} đã bị khóa.");
        if (!ok) return;
        ClMaTheMoi = "";
        await ChayAsync(TaiTheRanhAsync);
        await TaiVeThangAsync();
        await _print.MoHoaDonAsync(maHd);
    }

    // ---------- Lịch sử ----------
    [RelayCommand]
    private async Task XemLichSuAsync()
    {
        if (VeThangChon == null) return;
        var ve = VeThangChon;
        var tu = DateOnly.FromDateTime(LsTu ?? DateTime.Today.AddYears(-1));
        var den = DateOnly.FromDateTime(LsDen ?? DateTime.Today);
        await ChayAsync(async () =>
        {
            LsLuotGui.Clear();
            foreach (var l in await _svc.LayLuotGuiAsync(ve.MaXe, tu, den)) LsLuotGui.Add(l);
            LsHoaDon.Clear();
            foreach (var h in await _svc.LayHoaDonAsync(ve.MaThanhVien, tu, den)) LsHoaDon.Add(h);
        });
    }

    [RelayCommand]
    private async Task InHoaDonAsync()
    {
        if (LsHoaDonChon != null) await _print.MoHoaDonAsync(LsHoaDonChon.MaHoaDon);
    }
}
