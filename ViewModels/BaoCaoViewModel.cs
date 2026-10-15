using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Models.Dtos;
using QLBGX.Services;

namespace QLBGX.ViewModels;

/// <summary>Một cột trong biểu đồ doanh thu theo ngày.</summary>
public record CotBieuDo(string Nhan, decimal DoanhThu, double ChieuCao)
{
    public string GiaTri => MoneyFormatter.Format(DoanhThu);
}

public partial class BaoCaoViewModel : ViewModelBase
{
    private const double ChieuCaoToiDa = 180;

    private readonly BaoCaoService _svc;
    private readonly CaTrucService _caTruc;
    private readonly ExportService _export;
    private readonly Session _session;
    private readonly AppNavigator _nav;

    public BaoCaoViewModel(Notifier notify, BaoCaoService svc, CaTrucService caTruc, ExportService export,
        Session session, AppNavigator nav) : base(notify)
    {
        _svc = svc;
        _caTruc = caTruc;
        _export = export;
        _session = session;
        _nav = nav;
    }

    // Phân quyền tab: Doanh thu chỉ Quản lý; Theo ca: Quản lý (tất cả), Thu ngân (ca mình)
    public bool XemDoanhThu => _session.LaQuanLy;
    public bool XemTheoCa => Permissions.XemDoanhThu(_session);
    public string GhiChuTheoCa => _session.LaQuanLy ? "Tất cả các phiên chốt ca" : "Chỉ các phiên chốt ca của bạn";

    [ObservableProperty] private int _tabChon;

    // Doanh thu
    [ObservableProperty] private DateTime? _tuNgay = new DateTime(2026, 10, 1);
    [ObservableProperty] private DateTime? _denNgay = DateTime.Today;
    [ObservableProperty] private BaoCaoDoanhThuDto? _baoCao;
    [ObservableProperty] private string _tongDoanhThu = "";
    public ObservableCollection<CotBieuDo> BieuDo { get; } = [];

    public ObservableCollection<VwDoanhThuTheoCa> TheoCa { get; } = [];
    public ObservableCollection<VwTrangThaiKhuVuc> LapDay { get; } = [];
    public ObservableCollection<VwXeTonDong> TonDong { get; } = [];
    public ObservableCollection<VwLuotGuiLechBienSo> LechBienSo { get; } = [];
    public ObservableCollection<VwVeThangSapHetHan> VeThang { get; } = [];
    public ObservableCollection<LuotChoXuLy> ChoXuLy { get; } = [];

    [ObservableProperty] private VwLuotGuiLechBienSo? _lechChon;

    /// <summary>Thứ tự tab trong XAML.</summary>
    private static readonly string[] TenTab = ["DoanhThu", "TheoCa", "LapDay", "TonDong", "ChoXuLy", "LechBienSo", "VeThang"];

    public override async Task TaiAsync()
    {
        var thamSo = _nav.LayThamSo();
        var idx = thamSo == null ? -1 : Array.IndexOf(TenTab, thamSo);
        if (idx >= 0) TabChon = idx;
        else if (!XemDoanhThu && TabChon < 2) TabChon = XemTheoCa ? 1 : 2;

        await ChayAsync(async () =>
        {
            await TaiCanhBaoAsync();
            if (XemTheoCa) await TaiTheoCaCoreAsync();
            if (XemDoanhThu) await TaiDoanhThuCoreAsync();
        });
    }

    private async Task TaiCanhBaoAsync()
    {
        Nap(LapDay, await _svc.TrangThaiKhuVucAsync());
        Nap(TonDong, await _svc.XeTonDongAsync());
        Nap(LechBienSo, await _svc.LechBienSoAsync());
        Nap(VeThang, await _svc.VeThangSapHetHanAsync());
        Nap(ChoXuLy, await _svc.LuotChoXuLyAsync());
    }

    private static void Nap<T>(ObservableCollection<T> ds, IEnumerable<T> moi)
    {
        ds.Clear();
        foreach (var x in moi) ds.Add(x);
    }

    [RelayCommand]
    private Task LamMoiAsync() => ChayAsync(async () =>
    {
        await TaiCanhBaoAsync();
        if (XemTheoCa) await TaiTheoCaCoreAsync();
        if (XemDoanhThu) await TaiDoanhThuCoreAsync();
    });

    // ---------- Doanh thu ----------
    [RelayCommand]
    private Task XemDoanhThuAsync() => ChayAsync(TaiDoanhThuCoreAsync);

    private async Task TaiDoanhThuCoreAsync()
    {
        var tu = DateOnly.FromDateTime(TuNgay ?? DateTime.Today);
        var den = DateOnly.FromDateTime(DenNgay ?? DateTime.Today);
        if (den < tu) throw new LoiNghiepVuException("Đến ngày phải sau hoặc bằng từ ngày.");
        BaoCao = await _svc.BaoCaoDoanhThuAsync(tu, den);
        TongDoanhThu = MoneyFormatter.Format(BaoCao.TheoPhuongThuc.Sum(x => x.DoanhThu));

        var theoNgay = BaoCao.ChiTiet.GroupBy(x => x.Ngay)
            .Select(g => (Ngay: g.Key, Tien: g.Sum(x => x.DoanhThu))).OrderBy(x => x.Ngay).ToList();
        var max = theoNgay.Count == 0 ? 0 : theoNgay.Max(x => x.Tien);
        BieuDo.Clear();
        foreach (var (ngay, tien) in theoNgay)
            BieuDo.Add(new CotBieuDo(ngay.ToString("dd/MM"), tien, max == 0 ? 0 : (double)(tien / max) * ChieuCaoToiDa));
    }

    // ---------- Theo ca ----------
    [RelayCommand]
    private Task XemTheoCaAsync() => ChayAsync(TaiTheoCaCoreAsync);

    private async Task TaiTheoCaCoreAsync()
    {
        var tu = TuNgay == null ? (DateOnly?)null : DateOnly.FromDateTime(TuNgay.Value);
        var den = DenNgay == null ? (DateOnly?)null : DateOnly.FromDateTime(DenNgay.Value);
        Nap(TheoCa, await _caTruc.LayPhienAsync(_session.LaQuanLy ? null : _session.MaNV, false, tu, den));
    }

    // ---------- Xuất Excel/CSV ----------
    [RelayCommand]
    private void Xuat(string? bang)
    {
        try
        {
            string? file = bang switch
            {
                "DoanhThu" when BaoCao != null => _export.Xuat("DoanhThu",
                    new BangXuat("Chi tiết", BaoCao.ChiTiet), new BangXuat("Theo loại xe - khung", BaoCao.TheoLoaiXe),
                    new BangXuat("Theo phương thức", BaoCao.TheoPhuongThuc)),
                "TheoCa" => _export.Xuat("DoanhThuTheoCa", new BangXuat("Theo ca", TheoCa)),
                "LapDay" => _export.Xuat("TyLeLapDay", new BangXuat("Tỷ lệ lấp đầy", LapDay)),
                "TonDong" => _export.Xuat("XeTonDong", new BangXuat("Xe tồn đọng", TonDong)),
                "ChoXuLy" => _export.Xuat("LuotChoXuLy", new BangXuat("Chờ xử lý", ChoXuLy)),
                "LechBienSo" => _export.Xuat("LechBienSo", new BangXuat("Lệch biển số", LechBienSo)),
                "VeThang" => _export.Xuat("VeThangSapHetHan", new BangXuat("Vé tháng sắp hết hạn", VeThang)),
                _ => null,
            };
            if (file != null) Notify.ThanhCong("Đã xuất: " + file);
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }
}
