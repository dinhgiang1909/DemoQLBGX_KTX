using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;
using QLBGX.Views.Pages;

namespace QLBGX.ViewModels;

/// <summary>Một mục trong khối Cảnh báo; bấm vào mở màn hình tương ứng.</summary>
public record CanhBaoItem(string Ten, int SoLuong, MucTrangThai Muc, Type Trang, string? ThamSo);

public partial class DashboardViewModel : ViewModelBase
{
    /// <summary>Còn ít hơn số thẻ lượt Rảnh này thì cảnh báo vàng.</summary>
    private const int NguongTheRanh = 5;

    private readonly BaoCaoService _svc;
    private readonly Session _session;
    private readonly AppNavigator _nav;
    private readonly DispatcherTimer _timer;

    public DashboardViewModel(Notifier notify, BaoCaoService svc, Session session, AppNavigator nav) : base(notify)
    {
        _svc = svc;
        _session = session;
        _nav = nav;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += async (_, _) => await TaiCoreAsync(true);
    }

    public bool XemDoanhThu => _session.LaQuanLy;

    public ObservableCollection<VwTrangThaiKhuVuc> KhuVucs { get; } = [];
    public ObservableCollection<CanhBaoItem> CanhBaos { get; } = [];

    [ObservableProperty] private DateTime? _ngayXem = DateTime.Today;
    [ObservableProperty] private DashboardSoLieu? _soLieu;
    [ObservableProperty] private string _capNhatLuc = "";

    public override async Task TaiAsync()
    {
        _timer.Start();
        await TaiCoreAsync(false);
    }

    public override void RoiTrang() => _timer.Stop();

    partial void OnNgayXemChanged(DateTime? value) => _ = TaiCoreAsync(false);

    private async Task TaiCoreAsync(bool imLang)
    {
        if (DangXuLy) return;
        try
        {
            DangXuLy = true;
            var ngay = DateOnly.FromDateTime(NgayXem ?? DateTime.Today);
            SoLieu = await _svc.DashboardAsync(ngay);
            KhuVucs.Clear();
            foreach (var k in await _svc.TrangThaiKhuVucAsync()) KhuVucs.Add(k);

            CanhBaos.Clear();
            CanhBaos.Add(new CanhBaoItem("Xe tồn đọng", SoLieu.XeTonDong, SoLieu.XeTonDong > 0 ? MucTrangThai.Loi : MucTrangThai.HopLe,
                typeof(BaoCaoPage), "TonDong"));
            CanhBaos.Add(new CanhBaoItem("Lượt gửi \"Chờ xử lý\"", SoLieu.ChoXuLy, SoLieu.ChoXuLy > 0 ? MucTrangThai.Loi : MucTrangThai.HopLe,
                typeof(BaoCaoPage), "ChoXuLy"));
            if (_session.LaQuanLy)
                CanhBaos.Add(new CanhBaoItem("Phiên chốt ca \"Chờ xác nhận\"", SoLieu.ChotCaChoXacNhan,
                    SoLieu.ChotCaChoXacNhan > 0 ? MucTrangThai.CanhBao : MucTrangThai.HopLe, typeof(ChotCaPage), null));
            CanhBaos.Add(new CanhBaoItem("Vé tháng sắp / vừa hết hạn (±7 ngày)", SoLieu.VeThangSapHetHan,
                SoLieu.VeThangSapHetHan > 0 ? MucTrangThai.CanhBao : MucTrangThai.HopLe, typeof(BaoCaoPage), "VeThang"));
            // Mục 3.1.6: hết thẻ lượt Rảnh / thẻ "Đang sử dụng" mà không có lượt gửi
            CanhBaos.Add(new CanhBaoItem(SoLieu.TheLuotRanh == 0 ? "HẾT thẻ lượt Rảnh – bổ sung thẻ" : "Thẻ lượt Rảnh còn lại",
                SoLieu.TheLuotRanh,
                SoLieu.TheLuotRanh == 0 ? MucTrangThai.Loi : SoLieu.TheLuotRanh < NguongTheRanh ? MucTrangThai.CanhBao : MucTrangThai.HopLe,
                typeof(KhoThePage), "Rảnh"));
            CanhBaos.Add(new CanhBaoItem("Thẻ \"Đang sử dụng\" không có lượt gửi (lệch)", SoLieu.TheLech,
                SoLieu.TheLech > 0 ? MucTrangThai.Loi : MucTrangThai.HopLe, typeof(KhoThePage), TheService.LocLech));
            CapNhatLuc = "Cập nhật lúc " + DateTime.Now.ToString("HH:mm:ss") + " – tự làm mới mỗi 30 giây";
        }
        catch (Exception ex)
        {
            if (!imLang) Notify.Loi(ex);
        }
        finally
        {
            DangXuLy = false;
        }
    }

    [RelayCommand]
    private Task LamMoiAsync() => TaiCoreAsync(false);

    [RelayCommand]
    private void MoCanhBao(CanhBaoItem? item)
    {
        if (item != null) _nav.DenTrang(item.Trang, item.ThamSo);
    }

    [RelayCommand]
    private void DenTrang(string? ten)
    {
        switch (ten)
        {
            case "XeVao": _nav.DenTrang(typeof(XeVaoPage)); break;
            case "XeRa": _nav.DenTrang(typeof(XeRaPage)); break;
            case "LapDay": _nav.DenTrang(typeof(BaoCaoPage), "LapDay"); break;
        }
    }
}
