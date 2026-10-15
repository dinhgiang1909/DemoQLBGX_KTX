using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Helpers;
using QLBGX.Models;

namespace QLBGX.Services;

/// <summary>Dòng vé tháng trong danh sách của khách hàng.</summary>
public class VeThangRow
{
    public int MaThanhVien { get; init; }
    public int MaXe { get; init; }
    public string BienSo { get; init; } = "";
    public string MaLoaiXe { get; init; } = "";
    public string TenLoai { get; init; } = "";
    public string? MauSac { get; init; }
    public string MaThe { get; init; } = "";
    public DateOnly NgayDangKy { get; init; }
    public DateOnly NgayHetHan { get; init; }
    public string TrangThai { get; init; } = "";

    public int SoNgayConLai => NgayHetHan.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    /// <summary>Còn hạn / Sắp hết hạn (≤ 7 ngày) / Hết hạn / Khóa — dùng tô màu.</summary>
    public string TinhTrang => TrangThai == "Khóa" ? "Khóa"
        : SoNgayConLai < 0 ? "Hết hạn"
        : SoNgayConLai <= 7 ? "Sắp hết hạn"
        : "Còn hạn";
}

public record LuotGuiLichSu(long MaVe, DateTime ThoiGianVao, DateTime? ThoiGianRa, string MaThe, string MaKhuVuc,
    string TrangThai, string? BienSoRa, string? GhiChu);

public record HoaDonLichSu(long MaHoaDon, DateTime NgayTao, string LoaiHoaDon, int? SoThang, decimal TongTien,
    string PhuongThucTT, string MaNV);

public class VeThangService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    // ---------- Khách hàng (EF) ----------
    public async Task<List<KhachHang>> TimKhachHangAsync(string? tuKhoa)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.KhachHangs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = tuKhoa.Trim();
            var maThe = k.ToUpperInvariant();
            var bs = BienSoHelper.ChuanHoa(k);
            q = q.Where(x => x.TenKh.Contains(k) || x.MaDinhDanh.Contains(k) || (x.Sdt != null && x.Sdt.Contains(k))
                             || x.ThanhViens.Any(tv => tv.MaThe == maThe || (bs.Length >= 4 && tv.MaXeNavigation.BienSo.Contains(bs))));
        }
        return await q.OrderBy(x => x.TrangThai == "Ngừng").ThenBy(x => x.TenKh).ToListAsync();
    }

    /// <summary>Tìm vé tháng theo mã thẻ tháng (khớp đúng) hoặc biển số (khớp đúng sau chuẩn hóa) – dùng cho gia hạn.</summary>
    public async Task<(int MaKh, int MaThanhVien)?> TimVeThangTheoTheHoacBienSoAsync(string tuKhoa)
    {
        var maThe = tuKhoa.Trim().ToUpperInvariant();
        var bs = BienSoHelper.ChuanHoa(tuKhoa);
        if (maThe.Length == 0) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        var tv = await db.ThanhViens.AsNoTracking()
            .Where(t => t.MaThe == maThe || t.MaXeNavigation.BienSo == bs)
            .Select(t => new { t.MaKh, t.MaThanhVien }).FirstOrDefaultAsync();
        return tv == null ? null : (tv.MaKh, tv.MaThanhVien);
    }

    public async Task<KhachHang> LuuKhachHangAsync(KhachHang kh)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.KhachHangs.AnyAsync(x => x.MaDinhDanh == kh.MaDinhDanh && x.MaKh != kh.MaKh))
            throw new LoiNghiepVuException($"Mã định danh {kh.MaDinhDanh} đã tồn tại.");
        if (kh.MaKh == 0)
        {
            kh.TrangThai = "Hoạt động";
            db.KhachHangs.Add(kh);
        }
        else
        {
            var e = await db.KhachHangs.FirstAsync(x => x.MaKh == kh.MaKh);
            e.TenKh = kh.TenKh;
            e.MaDinhDanh = kh.MaDinhDanh;
            e.DoiTuong = kh.DoiTuong;
            e.DonVi = kh.DonVi;
            e.Sdt = kh.Sdt;
            e.Email = kh.Email;
            kh = e;
        }
        await db.SaveChangesAsync();
        return kh;
    }

    /// <summary>Xóa mềm: chỉ đổi TrangThai.</summary>
    public async Task DoiTrangThaiKhachHangAsync(int maKh, string trangThai)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.KhachHangs.FirstAsync(x => x.MaKh == maKh);
        e.TrangThai = trangThai;
        await db.SaveChangesAsync();
    }

    // ---------- Vé tháng ----------
    public async Task<List<VeThangRow>> LayVeThangAsync(int maKh)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ThanhViens.AsNoTracking().Where(tv => tv.MaKh == maKh)
            .OrderByDescending(tv => tv.NgayHetHan)
            .Select(tv => new VeThangRow
            {
                MaThanhVien = tv.MaThanhVien,
                MaXe = tv.MaXe,
                BienSo = tv.MaXeNavigation.BienSo,
                MaLoaiXe = tv.MaXeNavigation.MaLoaiXe,
                TenLoai = tv.MaXeNavigation.MaLoaiXeNavigation.TenLoai,
                MauSac = tv.MaXeNavigation.MauSac,
                MaThe = tv.MaThe,
                NgayDangKy = tv.NgayDangKy,
                NgayHetHan = tv.NgayHetHan,
                TrangThai = tv.TrangThai,
            }).ToListAsync();
    }

    public async Task<BangGium?> LayBangGiaHienHanhAsync(string maLoaiXe)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BangGia.AsNoTracking()
            .Where(b => b.MaLoaiXe == maLoaiXe && b.NgayApDung <= homNay)
            .OrderByDescending(b => b.NgayApDung).FirstOrDefaultAsync();
    }

    public async Task<(int MaThanhVien, long MaHoaDon)> DangKyAsync(int maKh, string bienSo, string maLoaiXe, string maThe,
        int soThang, string maNV, string? mauSac, string phuongThuc)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pTV = DbProc.Out("@MaThanhVien", SqlDbType.Int);
        var pHD = DbProc.Out("@MaHoaDon", SqlDbType.BigInt);
        await DbProc.ExecuteAsync(db, "dbo.sp_DangKyVeThang",
            DbProc.In("@MaKH", SqlDbType.Int, maKh),
            DbProc.In("@BienSo", SqlDbType.NVarChar, bienSo, 50),
            DbProc.In("@MaLoaiXe", SqlDbType.VarChar, maLoaiXe, 10),
            DbProc.In("@MaThe", SqlDbType.VarChar, maThe, 20),
            DbProc.In("@SoThang", SqlDbType.Int, soThang),
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@MauSac", SqlDbType.NVarChar, DbProc.NullNeuRong(mauSac), 30),
            DbProc.In("@PhuongThucTT", SqlDbType.NVarChar, phuongThuc, 30),
            pTV, pHD);
        return (pTV.GiaTri<int>(), pHD.GiaTri<long>());
    }

    public async Task<long> GiaHanAsync(int maThanhVien, int soThang, string maNV, string phuongThuc)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pHD = DbProc.Out("@MaHoaDon", SqlDbType.BigInt);
        await DbProc.ExecuteAsync(db, "dbo.sp_GiaHanVeThang",
            DbProc.In("@MaThanhVien", SqlDbType.Int, maThanhVien),
            DbProc.In("@SoThang", SqlDbType.Int, soThang),
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@PhuongThucTT", SqlDbType.NVarChar, phuongThuc, 30),
            pHD);
        return pHD.GiaTri<long>();
    }

    public async Task KhoaAsync(int maThanhVien, string maNV, string lyDo)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_KhoaVeThang",
            DbProc.In("@MaThanhVien", SqlDbType.Int, maThanhVien),
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@LyDo", SqlDbType.NVarChar, lyDo, 200));
    }

    public async Task MoKhoaAsync(int maThanhVien, string maNVQuanLy)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_MoKhoaVeThang",
            DbProc.In("@MaThanhVien", SqlDbType.Int, maThanhVien),
            DbProc.In("@MaNVQuanLy", SqlDbType.VarChar, maNVQuanLy, 10));
    }

    public async Task<long> CapLaiTheAsync(int maThanhVien, string maTheMoi, string maNV, string phuongThuc)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pHD = DbProc.Out("@MaHoaDon", SqlDbType.BigInt);
        await DbProc.ExecuteAsync(db, "dbo.sp_CapLaiTheThang",
            DbProc.In("@MaThanhVien", SqlDbType.Int, maThanhVien),
            DbProc.In("@MaTheMoi", SqlDbType.VarChar, maTheMoi, 20),
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@PhuongThucTT", SqlDbType.NVarChar, phuongThuc, 30),
            pHD);
        return pHD.GiaTri<long>();
    }

    // ---------- Lịch sử ----------
    public async Task<List<LuotGuiLichSu>> LayLuotGuiAsync(int maXe, DateOnly tu, DateOnly den)
    {
        var a = tu.ToDateTime(TimeOnly.MinValue);
        var b = den.AddDays(1).ToDateTime(TimeOnly.MinValue);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VeGuiXes.AsNoTracking()
            .Where(v => v.MaXe == maXe && v.ThoiGianVao >= a && v.ThoiGianVao < b)
            .OrderByDescending(v => v.ThoiGianVao)
            .Select(v => new LuotGuiLichSu(v.MaVe, v.ThoiGianVao, v.ThoiGianRa, v.MaThe, v.MaKhuVuc, v.TrangThai, v.BienSoRa, v.GhiChu))
            .ToListAsync();
    }

    public async Task<List<HoaDonLichSu>> LayHoaDonAsync(int maThanhVien, DateOnly tu, DateOnly den)
    {
        var a = tu.ToDateTime(TimeOnly.MinValue);
        var b = den.AddDays(1).ToDateTime(TimeOnly.MinValue);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.HoaDons.AsNoTracking()
            .Where(h => h.MaThanhVien == maThanhVien && h.NgayTao >= a && h.NgayTao < b)
            .OrderByDescending(h => h.NgayTao)
            .Select(h => new HoaDonLichSu(h.MaHoaDon, h.NgayTao, h.LoaiHoaDon, h.SoThang, h.TongTien, h.PhuongThucTt, h.MaNv))
            .ToListAsync();
    }
}
