using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class ChotCaViewModel : ViewModelBase
{
    private readonly CaTrucService _svc;
    private readonly Session _session;
    private readonly DialogService _dialog;
    private readonly PrintService _print;

    public ChotCaViewModel(Notifier notify, CaTrucService svc, Session session, DialogService dialog, PrintService print)
        : base(notify)
    {
        _svc = svc;
        _session = session;
        _dialog = dialog;
        _print = print;
    }

    public bool LaQuanLy => Permissions.XacNhanChotCa(_session);
    public string TenNhanVien => $"{_session.TenNV} ({_session.MaNV}) – {_session.ChucVu}";

    public ObservableCollection<VwDoanhThuTheoCa> PhienChoXacNhan { get; } = [];
    public ObservableCollection<VwDoanhThuTheoCa> PhienCuaToi { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChenhLech), nameof(ChenhLechHienThi), nameof(CoChenhLech))]
    private PhienHienTai? _phien;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChenhLech), nameof(ChenhLechHienThi), nameof(CoChenhLech))]
    private string _tienMatThucNop = "";

    [ObservableProperty] private string _ghiChu = "";
    [ObservableProperty] private VwDoanhThuTheoCa? _phienChoChon;
    [ObservableProperty] private VwDoanhThuTheoCa? _phienCuaToiChon;
    [ObservableProperty] private string? _ketQua;

    private decimal? SoTienNhap =>
        decimal.TryParse(TienMatThucNop.Replace(".", "").Replace(",", "").Replace(" ", "").Replace("đ", ""),
            NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Chênh lệch = thực nộp − doanh thu tiền mặt (chỉ để xem trước).</summary>
    public decimal? ChenhLech => Phien != null && SoTienNhap is { } n ? n - Phien.TienMat : null;
    public bool CoChenhLech => ChenhLech is { } c && c != 0;
    public string ChenhLechHienThi => ChenhLech is { } c ? (c > 0 ? "+" : "") + MoneyFormatter.Format(c) : "";

    public override async Task TaiAsync() => await ChayAsync(TaiCoreAsync);

    private async Task TaiCoreAsync()
    {
        Phien = await _svc.XemPhienHienTaiAsync(_session.MaNV!);
        if (string.IsNullOrEmpty(TienMatThucNop)) TienMatThucNop = Phien.TienMat.ToString("0", CultureInfo.InvariantCulture);
        PhienCuaToi.Clear();
        foreach (var p in (await _svc.LayPhienAsync(_session.MaNV)).Take(30)) PhienCuaToi.Add(p);
        PhienChoXacNhan.Clear();
        if (LaQuanLy)
            foreach (var p in await _svc.LayPhienAsync(chiChoXacNhan: true)) PhienChoXacNhan.Add(p);
    }

    [RelayCommand]
    private Task LamMoiAsync() => ChayAsync(TaiCoreAsync);

    [RelayCommand]
    private async Task ChotCaAsync()
    {
        if (Phien == null) return;
        if (SoTienNhap is not { } nop || nop < 0)
        {
            Notify.Loi("Nhập số tiền mặt thực nộp hợp lệ (số không âm).");
            return;
        }
        if (CoChenhLech && string.IsNullOrWhiteSpace(GhiChu))
        {
            Notify.Loi("Có chênh lệch tiền mặt: bắt buộc ghi chú lý do.");
            return;
        }
        if (!await _dialog.XacNhanAsync("Xác nhận chốt ca",
                $"Doanh thu tiền mặt: {MoneyFormatter.Format(Phien.TienMat)}\nThực nộp: {MoneyFormatter.Format(nop)}\n" +
                $"Chênh lệch: {ChenhLechHienThi}\nSau khi chốt, các hóa đơn sẽ được gắn vào phiên này.", "Chốt ca")) return;

        LichSuCaTruc? kq = null;
        var ok = await ChayAsync(async () => kq = await _svc.ChotCaAsync(_session.MaNV!, nop, GhiChu), "Đã chốt ca.");
        if (!ok || kq == null) return;
        KetQua = $"Phiên #{kq.MaLichSuCa}: {kq.SoHoaDon} hóa đơn – tổng {MoneyFormatter.Format(kq.TongDoanhThu)} – chênh lệch {MoneyFormatter.Format(kq.ChenhLech)} – {kq.TrangThai}";
        TienMatThucNop = "";
        GhiChu = "";
        await ChayAsync(TaiCoreAsync);
        _print.MoBienBanChotCa(kq);
    }

    [RelayCommand]
    private async Task InBienBanAsync(VwDoanhThuTheoCa? dong)
    {
        dong ??= PhienCuaToiChon ?? PhienChoChon;
        if (dong == null) return;
        LichSuCaTruc? p = null;
        if (await ChayAsync(async () => p = await _svc.LayMotPhienAsync(dong.MaLichSuCa)) && p != null)
            _print.MoBienBanChotCa(p);
    }

    [RelayCommand]
    private async Task XacNhanAsync()
    {
        if (!LaQuanLy || PhienChoChon == null) return;
        var p = PhienChoChon;
        var ghiChu = await _dialog.NhapAsync("Xác nhận chốt ca",
            $"Phiên #{p.MaLichSuCa} – {p.TenNv} – chênh lệch {MoneyFormatter.Format(p.ChenhLech)}. Ghi chú (tùy chọn):", false);
        if (ghiChu == null) return;
        if (await ChayAsync(() => _svc.XacNhanChotCaAsync(p.MaLichSuCa, _session.MaNV!, ghiChu), $"Đã xác nhận phiên #{p.MaLichSuCa}."))
            await ChayAsync(TaiCoreAsync);
    }
}
