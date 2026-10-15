using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QLBGX.Devices;
using QLBGX.Helpers;
using QLBGX.Models.Dtos;
using QLBGX.Services;

namespace QLBGX.ViewModels;

/// <summary>Một lựa chọn trong ComboBox khuyến mãi (Ma = null: không áp dụng).</summary>
public record LuaChonKhuyenMai(string? Ma, string Ten, decimal PhanTram)
{
    public string HienThi => Ma == null ? Ten : $"{Ma} – {Ten} (giảm {PhanTram:0.##}%)";
}

public partial class XeRaViewModel : ViewModelBase
{
    private readonly XeRaService _svc;
    private readonly IBienSoReader _reader;
    private readonly ICardReader _card;
    private readonly IBarrier _barrier;
    private readonly ImageStore _images;
    private readonly Session _session;
    private readonly DialogService _dialog;
    private readonly PrintService _print;

    public XeRaViewModel(Notifier notify, XeRaService svc, IBienSoReader reader, ICardReader card, IBarrier barrier,
        ImageStore images, Session session, DialogService dialog, PrintService print) : base(notify)
    {
        _svc = svc;
        _reader = reader;
        _card = card;
        _barrier = barrier;
        _images = images;
        _session = session;
        _dialog = dialog;
        _print = print;
    }

    public event EventHandler<string>? YeuCauFocus;

    private static readonly string ThuMucAnhMau = Path.Combine(AppContext.BaseDirectory, "AnhMau");

    private void OnBarrier(object? sender, bool mo) => Application.Current.Dispatcher.Invoke(() => BarrierMo = mo);
    public override void RoiTrang() => _barrier.TrangThaiThayDoi -= OnBarrier;

    public ObservableCollection<GoiYThe> GoiYThes { get; } = [];
    public ObservableCollection<LuaChonKhuyenMai> KhuyenMais { get; } = [];
    public string[] PhuongThucs { get; } = ["Tiền mặt", "Chuyển khoản"];

    // Tìm lượt gửi
    [ObservableProperty] private bool _matThe;
    [ObservableProperty] private string _maTheNhap = "";
    [ObservableProperty] private string _bienSoTimKiem = "";

    // Lượt gửi tìm được
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLuot), nameof(ThoiGianDaGui), nameof(LechBienSo), nameof(CanXacNhanGiayTo),
        nameof(TongDuKien), nameof(PhiMatTheHienThi), nameof(ThongBaoQuyen), nameof(CoTheThanhToan),
        nameof(CoTheGiaiQuyet), nameof(CoTheGiuXe), nameof(HienThuTien), nameof(LaTheThangMatThe))]
    private TraCuuXeRaDto? _luot;

    private LuotGuiPhu? _phu;

    // Lúc ra
    [ObservableProperty] private string? _duongDanAnhRa;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LechBienSo), nameof(CanXacNhanGiayTo))]
    private string _bienSoRa = "";
    [ObservableProperty] private bool _xacNhanGiayTo;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SoTienGiamDuKien), nameof(TongDuKien))]
    private LuaChonKhuyenMai? _khuyenMaiChon;
    [ObservableProperty] private string _phuongThucChon = "Tiền mặt";
    [ObservableProperty] private string _ghiChu = "";

    [ObservableProperty] private bool _barrierMo;
    [ObservableProperty] private string? _ketQua;
    [ObservableProperty] private MucTrangThai _mucKetQua;

    public bool CoLuot => Luot != null;

    public string ThoiGianDaGui
    {
        get
        {
            if (Luot == null) return "";
            var t = DateTime.Now - Luot.ThoiGianVao;
            return t.TotalDays >= 1 ? $"{(int)t.TotalDays} ngày {t.Hours} giờ {t.Minutes} phút" : $"{t.Hours} giờ {t.Minutes} phút";
        }
    }

    public bool LechBienSo => Luot != null && !string.IsNullOrWhiteSpace(BienSoRa) && !BienSoHelper.GiongNhau(BienSoRa, Luot.BienSoVao);
    public bool CanXacNhanGiayTo => Luot != null && (LechBienSo || MatThe);
    public bool HienThuTien => Luot?.PhaiThuTien == true;
    public bool LaTheThangMatThe => MatThe && _phu?.LoaiTheLuot == "Thẻ tháng";

    /// <summary>Chỉ để hiển thị: tiền gốc × % / 100, làm tròn đồng. Số thực thu lấy từ sp_XuLyXeRa.</summary>
    public decimal SoTienGiamDuKien => Luot?.PhaiThuTien == true && KhuyenMaiChon is { Ma: not null } km
        ? Math.Round((Luot.TongTienGoc ?? 0) * km.PhanTram / 100, 0, MidpointRounding.AwayFromZero)
        : 0;

    public decimal PhiMatTheDuKien => MatThe && Luot?.PhaiThuTien == true ? _phu?.PhiMatVe ?? 0 : 0;
    public string? PhiMatTheHienThi => PhiMatTheDuKien > 0 ? MoneyFormatter.Format(PhiMatTheDuKien) : null;

    public decimal TongDuKien => Luot?.PhaiThuTien == true ? (Luot.TongTienGoc ?? 0) - SoTienGiamDuKien + PhiMatTheDuKien : 0;

    public string? ThongBaoQuyen
    {
        get
        {
            if (Luot == null) return null;
            if (Luot.ChoXuLy)
                return _session.LaQuanLy
                    ? "Lượt gửi đang bị giữ (Chờ xử lý). Quản lý kiểm tra giấy tờ rồi bấm \"Giải quyết & cho ra\"."
                    : "Lượt gửi đang bị giữ – chỉ Quản lý được giải quyết.";
            if (LaTheThangMatThe)
                return "Thành viên mất thẻ tháng: cấp lại thẻ ở màn hình Vé tháng rồi quẹt thẻ mới.";
            if (Luot.PhaiThuTien && !Permissions.XeRaThuTien(_session))
                return "Lượt gửi phải thu tiền – chuyển Thu ngân xử lý.";
            return null;
        }
    }

    public bool CoTheThanhToan => Luot != null && !Luot.ChoXuLy && (!Luot.PhaiThuTien || Permissions.XeRaThuTien(_session));
    public bool CoTheGiaiQuyet => Luot?.ChoXuLy == true && Permissions.GiaiQuyetChoXuLy(_session);
    public bool CoTheGiuXe => Luot != null && !Luot.ChoXuLy;

    partial void OnMatTheChanged(bool value)
    {
        Luot = null;
        OnPropertyChanged(nameof(CanXacNhanGiayTo));
        YeuCauFocus?.Invoke(this, value ? "BienSoTim" : "The");
    }

    public override async Task TaiAsync()
    {
        _barrier.TrangThaiThayDoi -= OnBarrier;
        _barrier.TrangThaiThayDoi += OnBarrier;
        await ChayAsync(TaiGoiYAsync);
        YeuCauFocus?.Invoke(this, "The");
    }

    private async Task TaiGoiYAsync()
    {
        var ds = await _card.LayGoiYAsync(NguCanhQuetThe.XeRa);
        GoiYThes.Clear();
        foreach (var g in ds) GoiYThes.Add(g);
    }

    // ---------- 1. Tìm lượt gửi ----------
    [RelayCommand]
    private async Task TimLuotAsync()
    {
        var maThe = MatThe ? null : _card.ChuanHoaMaThe(MaTheNhap);
        var bienSo = MatThe ? BienSoHelper.ChuanHoa(BienSoTimKiem) : null;
        if (!MatThe) MaTheNhap = maThe!;
        if (string.IsNullOrEmpty(maThe) && string.IsNullOrEmpty(bienSo))
        {
            Notify.Loi(MatThe ? "Nhập biển số để tìm lượt gửi." : "Quẹt thẻ để tìm lượt gửi.");
            return;
        }

        Luot = null;
        _phu = null;
        KetQua = null;
        await ChayAsync(async () =>
        {
            var luot = await _svc.TraCuuAsync(maThe, bienSo);
            _phu = await _svc.LayThongTinPhuAsync(luot.MaVe);
            KhuyenMais.Clear();
            KhuyenMais.Add(new LuaChonKhuyenMai(null, "Không áp dụng", 0));
            if (luot.PhaiThuTien)
                foreach (var k in await _svc.LayKhuyenMaiApDungAsync(_phu.MaLoaiXe, luot.KhungGui))
                    KhuyenMais.Add(new LuaChonKhuyenMai(k.MaKhuyenMai, k.TenChuongTrinh, k.PhanTramGiam));
            KhuyenMaiChon = KhuyenMais[0];
            XacNhanGiayTo = false;
            Luot = luot;
            OnPropertyChanged(nameof(PhiMatTheHienThi));
            OnPropertyChanged(nameof(TongDuKien));
        });
        if (Luot != null) YeuCauFocus?.Invoke(this, "BienSoRa");
    }

    // ---------- 2. Chụp ảnh lúc ra ----------
    [RelayCommand]
    private async Task ChupAnhAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh từ camera làn ra (mô phỏng)",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            InitialDirectory = Directory.Exists(ThuMucAnhMau) ? ThuMucAnhMau : null,
        };
        if (dlg.ShowDialog() != true) return;
        DuongDanAnhRa = dlg.FileName;
        var bs = await _reader.DocBienSoAsync(dlg.FileName);
        if (bs.Length > 0) BienSoRa = bs;
        else Notify.CanhBao("Không đọc được biển số từ ảnh, vui lòng nhập tay.");
        YeuCauFocus?.Invoke(this, "BienSoRa");
    }

    // ---------- 6. Thanh toán / cho ra ----------
    [RelayCommand]
    private Task ThanhToanAsync()
    {
        if (Luot == null)
        {
            Notify.Loi("Chưa tìm được lượt gửi.");
            return Task.CompletedTask;
        }
        if (Luot.ChoXuLy)
        {
            Notify.CanhBao(ThongBaoQuyen!);
            return Task.CompletedTask;
        }
        if (!CoTheThanhToan)
        {
            Notify.CanhBao(ThongBaoQuyen ?? "Không thể cho xe ra.");
            return Task.CompletedTask;
        }
        return ChoRaAsync(XacNhanGiayTo);
    }

    /// <summary>Quản lý giải quyết lượt "Chờ xử lý": gọi sp_XuLyXeRa với @XacNhanGiayTo = 1.</summary>
    [RelayCommand]
    private async Task GiaiQuyetAsync()
    {
        if (!CoTheGiaiQuyet) return;
        if (!await _dialog.XacNhanAsync("Giải quyết lượt gửi bị giữ",
                "Xác nhận đã kiểm tra giấy tờ xe và cho xe ra?", "Cho ra")) return;
        await ChoRaAsync(true);
    }

    private async Task ChoRaAsync(bool xacNhanGiayTo)
    {
        if (string.IsNullOrWhiteSpace(BienSoRa))
        {
            Notify.Loi("Nhập biển số lúc ra (chụp ảnh hoặc gõ tay).");
            YeuCauFocus?.Invoke(this, "BienSoRa");
            return;
        }
        var luot = Luot!;
        string? anh = null;
        XeRaKetQua? kq = null;
        var ok = await ChayAsync(async () =>
        {
            if (DuongDanAnhRa != null) anh = _images.Luu(DuongDanAnhRa, "ra");
            try
            {
                kq = await _svc.XuLyXeRaAsync(_session.MaNV!, BienSoRa,
                    MatThe ? null : MaTheNhap, MatThe ? BienSoHelper.ChuanHoa(BienSoTimKiem) : null,
                    anh, luot.PhaiThuTien ? KhuyenMaiChon?.Ma : null, PhuongThucChon,
                    xacNhanGiayTo, false, GhiChu);
            }
            catch
            {
                XoaAnh(anh);
                throw;
            }
        });
        if (!ok || kq == null)
        {
            KetQua = "Không cho ra được – barrier không mở.";
            MucKetQua = MucTrangThai.Loi;
            return;
        }

        KetQua = $"{kq.KetQua}  (Tổng: {MoneyFormatter.Format(kq.TongTien)})";
        MucKetQua = MucTrangThai.HopLe;
        Notify.ThanhCong(kq.KetQua);
        _ = _barrier.MoAsync();
        LamMoiForm();
        try { await TaiGoiYAsync(); } catch (Exception ex) { Notify.Loi(ex); }
        if (kq.MaHoaDon is long maHd) await _print.MoHoaDonAsync(maHd);
    }

    // ---------- 7. Giữ xe ----------
    [RelayCommand]
    private async Task GiuXeAsync()
    {
        if (Luot == null || Luot.ChoXuLy) return;
        // QT15 / mục 3.1.5b: giữ xe phải lưu biển số đọc lúc ra (không lấy biển số vào thay thế)
        if (string.IsNullOrWhiteSpace(BienSoRa))
        {
            Notify.Loi("Giữ xe phải lưu biển số lúc ra: chụp ảnh (F2) hoặc gõ biển số ra trước.");
            YeuCauFocus?.Invoke(this, "BienSoRa");
            return;
        }
        var lyDo = await _dialog.NhapAsync("Giữ xe (Chờ xử lý)", "Lý do giữ xe (bắt buộc)", true, GhiChu);
        if (lyDo == null) return;

        string? anh = null;
        XeRaKetQua? kq = null;
        var ok = await ChayAsync(async () =>
        {
            if (DuongDanAnhRa != null) anh = _images.Luu(DuongDanAnhRa, "ra");
            try
            {
                kq = await _svc.XuLyXeRaAsync(_session.MaNV!, BienSoRa,
                    MatThe ? null : MaTheNhap, MatThe ? BienSoHelper.ChuanHoa(BienSoTimKiem) : null,
                    anh, null, PhuongThucChon, XacNhanGiayTo, true, lyDo);
            }
            catch
            {
                XoaAnh(anh);
                throw;
            }
        });
        if (!ok || kq == null) return;
        KetQua = kq.KetQua;
        MucKetQua = MucTrangThai.CanhBao;
        Notify.CanhBao(kq.KetQua);
        LamMoiForm();
        try { await TaiGoiYAsync(); } catch (Exception ex) { Notify.Loi(ex); }
    }

    private static void XoaAnh(string? tuongDoi)
    {
        var full = ImageStore.DuongDanDayDu(tuongDoi);
        if (full == null) return;
        try { File.Delete(full); } catch { /* bỏ qua */ }
    }

    [RelayCommand]
    private void LamMoi()
    {
        LamMoiForm();
        KetQua = null;
    }

    private void LamMoiForm()
    {
        Luot = null;
        _phu = null;
        MaTheNhap = "";
        BienSoTimKiem = "";
        BienSoRa = "";
        DuongDanAnhRa = null;
        XacNhanGiayTo = false;
        GhiChu = "";
        PhuongThucChon = "Tiền mặt";
        KhuyenMais.Clear();
        MatThe = false;
        YeuCauFocus?.Invoke(this, "The");
    }
}
