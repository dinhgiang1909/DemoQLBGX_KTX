namespace QLBGX.Models.Dtos;

/// <summary>OUTPUT của sp_XuLyXeVao.</summary>
public record XeVaoKetQua(long MaVe, string MaKhuVuc, string LoaiVe);

/// <summary>Dòng kết quả của sp_TraCuuXeRa.</summary>
public class TraCuuXeRaDto
{
    public long MaVe { get; init; }
    public string MaThe { get; init; } = "";
    public string BienSoVao { get; init; } = "";
    public string TenLoai { get; init; } = "";
    public string MaKhuVuc { get; init; } = "";
    public DateTime ThoiGianVao { get; init; }
    public string? HinhAnhVao { get; init; }
    public string TrangThai { get; init; } = "";
    public string LoaiVe { get; init; } = "";
    public string? KhungGui { get; init; }
    public int? SoNgay { get; init; }
    public decimal? DonGia { get; init; }
    public decimal? TongTienGoc { get; init; }
    public string? GhiChu { get; init; }

    /// <summary>fn_TinhPhiLuotGui chỉ trả bảng giá (DonGia) khi phải thu tiền.</summary>
    public bool PhaiThuTien => DonGia != null;
    public bool ChoXuLy => TrangThai == "Chờ xử lý";
}

/// <summary>OUTPUT của sp_XuLyXeRa.</summary>
public record XeRaKetQua(long? MaHoaDon, decimal TongTien, string KetQua);

/// <summary>Thông tin in hóa đơn (đọc từ HoaDon).</summary>
public class HoaDonInDto
{
    public long MaHoaDon { get; init; }
    public string LoaiHoaDon { get; init; } = "";
    public DateTime NgayTao { get; init; }
    public string? BienSo { get; init; }
    public string? TenLoaiXe { get; init; }
    public string? TenKhachHang { get; init; }
    public string? MaThe { get; init; }
    public DateTime? ThoiGianVao { get; init; }
    public DateTime? ThoiGianRa { get; init; }
    public string? KhungGui { get; init; }
    public int? SoNgay { get; init; }
    public int? SoThang { get; init; }
    public DateOnly? NgayHetHan { get; init; }
    public decimal TongTienGoc { get; init; }
    public decimal SoTienGiam { get; init; }
    public string? KhuyenMai { get; init; }
    public decimal TienPhat { get; init; }
    public decimal TongTien { get; init; }
    public string PhuongThucTT { get; init; } = "";
    public string NhanVien { get; init; } = "";
}

public class DoanhThuChiTietDto
{
    public DateOnly Ngay { get; init; }
    public string LoaiHoaDon { get; init; } = "";
    public string? MaLoaiXe { get; init; }
    public string? KhungGui { get; init; }
    public string PhuongThucTT { get; init; } = "";
    public int SoHoaDon { get; init; }
    public decimal TongTienGoc { get; init; }
    public decimal TongGiam { get; init; }
    public decimal TongPhat { get; init; }
    public decimal DoanhThu { get; init; }
}

public class DoanhThuNhomDto
{
    public string? MaLoaiXe { get; init; }
    public string LoaiHoaDon { get; init; } = "";
    public string? KhungGui { get; init; }
    public int SoHoaDon { get; init; }
    public decimal DoanhThu { get; init; }
}

public class DoanhThuPhuongThucDto
{
    public string PhuongThucTT { get; init; } = "";
    public int SoHoaDon { get; init; }
    public decimal DoanhThu { get; init; }
}

/// <summary>3 bảng kết quả của sp_BaoCaoDoanhThu.</summary>
public class BaoCaoDoanhThuDto
{
    public List<DoanhThuChiTietDto> ChiTiet { get; } = [];
    public List<DoanhThuNhomDto> TheoLoaiXe { get; } = [];
    public List<DoanhThuPhuongThucDto> TheoPhuongThuc { get; } = [];
}
