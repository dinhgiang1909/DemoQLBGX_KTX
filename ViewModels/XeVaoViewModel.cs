using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QLBGX.Devices;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class XeVaoViewModel : ViewModelBase
{
    private readonly XeVaoService _svc;
    private readonly IBienSoReader _reader;
    private readonly ICardReader _card;
    private readonly IBarrier _barrier;
    private readonly ImageStore _images;
    private readonly Session _session;
    private TraCuuBienSoInfo? _traCuu;

    public XeVaoViewModel(Notifier notify, XeVaoService svc, IBienSoReader reader, ICardReader card, IBarrier barrier,
        ImageStore images, Session session) : base(notify)
    {
        _svc = svc;
        _reader = reader;
        _card = card;
        _barrier = barrier;
        _images = images;
        _session = session;
        _barrier.TrangThaiThayDoi += OnBarrier;
    }

    private void OnBarrier(object? sender, bool mo) => Application.Current.Dispatcher.Invoke(() => BarrierMo = mo);

    public override void RoiTrang() => _barrier.TrangThaiThayDoi -= OnBarrier;

    /// <summary>View lắng nghe để đặt con trỏ: "BienSo" hoặc "The".</summary>
    public event EventHandler<string>? YeuCauFocus;

    private static readonly string ThuMucAnhMau = Path.Combine(AppContext.BaseDirectory, "AnhMau");

    public ObservableCollection<LoaiXe> LoaiXes { get; } = [];
    public ObservableCollection<GoiYThe> GoiYThes { get; } = [];
    public ObservableCollection<LuotVaoGanDay> LuotGanDay { get; } = [];
    public ObservableCollection<VwTrangThaiKhuVuc> KhuVucs { get; } = [];

    [ObservableProperty] private LoaiXe? _loaiXeChon;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BienSoHienThi))] private string _bienSo = "";
    [ObservableProperty] private string? _duongDanAnh;
    [ObservableProperty] private string _maTheNhap = "";
    [ObservableProperty] private string _ghiChu = "";
    [ObservableProperty] private bool _barrierMo;
    [ObservableProperty] private string? _thongBaoKhoThe;
    [ObservableProperty] private MucTrangThai _mucKhoThe;

    // Gợi ý theo biển số
    [ObservableProperty] private string? _thongBaoBienSo;
    [ObservableProperty] private MucTrangThai _mucBienSo;

    // Kết quả quẹt thẻ
    [ObservableProperty] private string? _thongBaoThe;
    [ObservableProperty] private MucTrangThai _mucThe;
    [ObservableProperty] private VeThangInfo? _veThang;
    [ObservableProperty] private string? _thongBaoKhopXe;
    [ObservableProperty] private MucTrangThai _mucKhopXe;

    [ObservableProperty] private BangGium? _bangGia;
    [ObservableProperty] private string? _ketQua;
    [ObservableProperty] private MucTrangThai _mucKetQua;

    public string BienSoHienThi => string.IsNullOrWhiteSpace(BienSo) ? "" : "Chuẩn hóa: " + BienSoHelper.HienThi(BienSo) +
                                                                          "  (" + BienSoHelper.ChuanHoa(BienSo) + ")";

    public override async Task TaiAsync()
    {
        _barrier.TrangThaiThayDoi -= OnBarrier;
        _barrier.TrangThaiThayDoi += OnBarrier;
        await ChayAsync(async () =>
        {
            var lx = await _svc.LayLoaiXeHoatDongAsync();
            LoaiXes.Clear();
            foreach (var l in lx) LoaiXes.Add(l);
            await TaiDanhSachAsync();
        });
        YeuCauFocus?.Invoke(this, "BienSo");
    }

    private async Task TaiDanhSachAsync()
    {
        var goiY = await _card.LayGoiYAsync(NguCanhQuetThe.XeVao);
        GoiYThes.Clear();
        foreach (var g in goiY) GoiYThes.Add(g);

        var ranh = await _svc.DemTheLuotRanhAsync();
        ThongBaoKhoThe = ranh switch
        {
            0 => "HẾT thẻ lượt \"Rảnh\": không phát được thẻ cho xe vãng lai, barrier không mở. Báo Quản lý bổ sung thẻ hoặc thu hồi thẻ thất lạc.",
            < 5 => $"Chỉ còn {ranh} thẻ lượt \"Rảnh\" – báo Quản lý bổ sung thẻ.",
            _ => null,
        };
        MucKhoThe = ranh == 0 ? MucTrangThai.Loi : MucTrangThai.CanhBao;

        LuotGanDay.Clear();
        foreach (var l in await _svc.LayLuotVaoGanNhatAsync()) LuotGanDay.Add(l);
        KhuVucs.Clear();
        foreach (var k in await _svc.LayTrangThaiKhuVucAsync()) KhuVucs.Add(k);
    }

    partial void OnLoaiXeChonChanged(LoaiXe? value)
    {
        CapNhatThongBaoBienSo();
        _ = TaiBangGiaAsync();
    }

    private async Task TaiBangGiaAsync()
    {
        try
        {
            BangGia = LoaiXeChon == null ? null
                : await _svc.LayBangGiaAsync(LoaiXeChon.MaLoaiXe, DateOnly.FromDateTime(DateTime.Today));
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }

    // ---------- 1. Chụp ảnh ----------
    [RelayCommand]
    private async Task ChupAnhAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh từ camera (mô phỏng)",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            InitialDirectory = Directory.Exists(ThuMucAnhMau) ? ThuMucAnhMau : null,
        };
        if (dlg.ShowDialog() != true) return;
        DuongDanAnh = dlg.FileName;

        DangXuLy = true;
        try
        {
            var bs = await _reader.DocBienSoAsync(dlg.FileName);
            if (bs.Length > 0)
            {
                BienSo = bs;
                await TraCuuBienSoAsync();
            }
            else
            {
                Notify.CanhBao("Không đọc được biển số từ ảnh, vui lòng nhập tay.");
            }
        }
        finally
        {
            DangXuLy = false;
        }
        YeuCauFocus?.Invoke(this, string.IsNullOrEmpty(BienSo) ? "BienSo" : "The");
    }

    // ---------- 3. Tra cứu nhanh theo biển số ----------
    [RelayCommand]
    private async Task TraCuuBienSoAsync()
    {
        _traCuu = null;
        var bs = BienSoHelper.ChuanHoa(BienSo);
        if (bs.Length < 5)
        {
            CapNhatThongBaoBienSo();
            return;
        }
        try
        {
            _traCuu = await _svc.TraCuuBienSoAsync(bs);
            // Xe đã biết mà chưa chọn loại xe → tự chọn đúng loại đã lưu
            if (_traCuu.DaCoXe && LoaiXeChon == null)
                LoaiXeChon = LoaiXes.FirstOrDefault(l => l.MaLoaiXe == _traCuu.MaLoaiXe);
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
        CapNhatThongBaoBienSo();
        KiemTraKhopXe();
    }

    private void CapNhatThongBaoBienSo()
    {
        var t = _traCuu;
        if (t == null || !t.DaCoXe)
        {
            ThongBaoBienSo = t == null ? null : "Xe mới (chưa có trong hệ thống).";
            MucBienSo = MucTrangThai.BinhThuong;
            return;
        }
        var dong = new List<string>();
        var muc = MucTrangThai.HopLe;
        if (t.MaVeTrongBai != null)
        {
            dong.Add($"Xe đang có lượt gửi #{t.MaVeTrongBai} trong bãi – không thể vào lần nữa (QT9).");
            muc = MucTrangThai.Loi;
        }
        if (LoaiXeChon != null && LoaiXeChon.MaLoaiXe != t.MaLoaiXe)
        {
            dong.Add($"Biển số này đã lưu với loại \"{t.TenLoai}\", khác loại đang chọn.");
            if (muc != MucTrangThai.Loi) muc = MucTrangThai.CanhBao;
        }
        if (t.VeThang is { } vt)
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            if (vt.TrangThai == "Hoạt động" && vt.NgayHetHan >= homNay)
            {
                dong.Add($"Xe có vé tháng – thẻ {vt.MaThe} – hạn đến {vt.NgayHetHan:dd/MM/yyyy} – mời quẹt thẻ tháng.");
            }
            else
            {
                dong.Add($"Vé tháng không hợp lệ ({(vt.TrangThai == "Khóa" ? "bị khóa" : "hết hạn " + vt.NgayHetHan.ToString("dd/MM/yyyy"))}) – phát thẻ lượt, tính như vé lượt.");
                if (muc != MucTrangThai.Loi) muc = MucTrangThai.CanhBao;
            }
        }
        if (dong.Count == 0) dong.Add($"Xe đã từng gửi ({t.TenLoai}).");
        ThongBaoBienSo = string.Join(Environment.NewLine, dong);
        MucBienSo = muc;
    }

    // ---------- 4. Quẹt thẻ ----------
    [RelayCommand]
    private async Task QuetTheAsync()
    {
        var ma = _card.ChuanHoaMaThe(MaTheNhap);
        MaTheNhap = ma;
        VeThang = null;
        ThongBaoKhopXe = null;
        if (ma.Length == 0)
        {
            ThongBaoThe = null;
            return;
        }
        try
        {
            var the = await _svc.TraCuuTheAsync(ma);
            if (the == null)
            {
                ThongBaoThe = $"Thẻ {ma} không tồn tại trong kho thẻ.";
                MucThe = MucTrangThai.Loi;
                return;
            }
            if (the.TrangThai == "Khóa")
            {
                ThongBaoThe = $"Thẻ {ma} đang bị khóa, không được sử dụng.";
                MucThe = MucTrangThai.Loi;
                return;
            }
            if (the.LoaiThe == "Thẻ lượt")
            {
                ThongBaoThe = the.TrangThai == "Rảnh" ? "Thẻ lượt – Rảnh"
                    : the.CoLuotTrongBai ? "Thẻ lượt – Đang dùng (đang gắn với xe trong bãi), dùng thẻ khác."
                    : "Thẻ lượt \"Đang sử dụng\" nhưng không có lượt gửi nào trong bãi (dữ liệu lệch) – dùng thẻ khác và báo Quản lý kiểm tra.";
                MucThe = the.TrangThai == "Rảnh" ? MucTrangThai.HopLe : MucTrangThai.Loi;
                return;
            }
            ThongBaoThe = "Thẻ tháng";
            MucThe = MucTrangThai.HopLe;
            VeThang = the.VeThang;
            if (VeThang == null)
            {
                ThongBaoThe = the.TrangThai == "Đang sử dụng"
                    ? "Thẻ tháng \"Đang sử dụng\" nhưng không gắn vé tháng nào (dữ liệu lệch) – báo Quản lý kiểm tra."
                    : "Thẻ tháng chưa gắn với vé tháng nào.";
                MucThe = MucTrangThai.Loi;
            }
            KiemTraKhopXe();
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
        }
    }

    /// <summary>So biển số đăng ký của thẻ tháng với biển số trong ô (cả hai đã chuẩn hóa).</summary>
    private void KiemTraKhopXe()
    {
        if (VeThang is not { } vt) return;
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (vt.TrangThai == "Khóa")
        {
            ThongBaoKhopXe = "Vé tháng đang bị khóa – phát thẻ lượt, tính như vé lượt.";
            MucKhopXe = MucTrangThai.Loi;
        }
        else if (vt.NgayHetHan < homNay)
        {
            ThongBaoKhopXe = $"Vé tháng đã hết hạn ngày {vt.NgayHetHan:dd/MM/yyyy} – phát thẻ lượt, tính như vé lượt.";
            MucKhopXe = MucTrangThai.Loi;
        }
        else if (string.IsNullOrWhiteSpace(BienSo))
        {
            ThongBaoKhopXe = "Chưa có biển số để đối chiếu.";
            MucKhopXe = MucTrangThai.CanhBao;
        }
        else if (BienSoHelper.GiongNhau(vt.BienSo, BienSo))
        {
            ThongBaoKhopXe = "Khớp xe đăng ký.";
            MucKhopXe = MucTrangThai.HopLe;
        }
        else
        {
            ThongBaoKhopXe = $"Thẻ này thuộc xe {BienSoHelper.HienThi(vt.BienSo)}, không phải xe đang vào.";
            MucKhopXe = MucTrangThai.Loi;
        }
    }

    // ---------- 7. Tạo lượt gửi ----------
    [RelayCommand]
    private async Task TaoLuotGuiAsync()
    {
        if (DangXuLy) return;
        if (string.IsNullOrWhiteSpace(BienSo))
        {
            Notify.Loi("Vui lòng nhập biển số.");
            YeuCauFocus?.Invoke(this, "BienSo");
            return;
        }
        if (LoaiXeChon == null)
        {
            Notify.Loi("Vui lòng chọn loại xe.");
            return;
        }
        var maThe = _card.ChuanHoaMaThe(MaTheNhap);
        if (maThe.Length == 0)
        {
            Notify.Loi("Vui lòng quẹt thẻ.");
            YeuCauFocus?.Invoke(this, "The");
            return;
        }

        string? anhLuu = null;
        var ok = await ChayAsync(async () =>
        {
            if (DuongDanAnh != null) anhLuu = _images.Luu(DuongDanAnh, "vao");
            try
            {
                var kq = await _svc.XuLyXeVaoAsync(BienSo, LoaiXeChon.MaLoaiXe, maThe, _session.MaNV!, anhLuu, GhiChu);
                KetQua = $"Lượt #{kq.MaVe} – Khu vực {kq.MaKhuVuc} – {kq.LoaiVe} – xe {BienSoHelper.HienThi(BienSo)}";
                MucKetQua = MucTrangThai.HopLe;
            }
            catch
            {
                XoaAnh(anhLuu);
                throw;
            }
        });

        if (ok)
        {
            Notify.ThanhCong(KetQua!);
            _ = _barrier.MoAsync();
            LamMoiForm();
            try { await TaiDanhSachAsync(); } catch (Exception ex) { Notify.Loi(ex); }
        }
        else
        {
            KetQua = "Không tạo được lượt gửi – barrier không mở.";
            MucKetQua = MucTrangThai.Loi;
        }
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
        BienSo = "";
        DuongDanAnh = null;
        MaTheNhap = "";
        GhiChu = "";
        _traCuu = null;
        ThongBaoBienSo = null;
        ThongBaoThe = null;
        VeThang = null;
        ThongBaoKhopXe = null;
        YeuCauFocus?.Invoke(this, "BienSo");
    }

    [RelayCommand]
    private async Task TaiLaiAsync() => await ChayAsync(TaiDanhSachAsync);
}
