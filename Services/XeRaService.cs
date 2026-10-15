using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Models.Dtos;

namespace QLBGX.Services;

/// <summary>Thông tin bổ sung của lượt gửi để hiển thị (không tính tiền).</summary>
public record LuotGuiPhu(string MaLoaiXe, string LoaiTheLuot, decimal PhiMatVe);

public class XeRaService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    /// <summary>sp_TraCuuXeRa: chỉ đọc, ném 50020 nếu không tìm thấy.</summary>
    public async Task<TraCuuXeRaDto> TraCuuAsync(string? maThe, string? bienSo)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var cmd = await DbProc.TaoLenhAsync(db, "dbo.sp_TraCuuXeRa",
            DbProc.In("@MaThe", SqlDbType.VarChar, DbProc.NullNeuRong(maThe), 20),
            DbProc.In("@BienSo", SqlDbType.NVarChar, DbProc.NullNeuRong(bienSo), 50));
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
            throw new LoiNghiepVuException("Không tìm thấy lượt gửi đang trong bãi.");
        return new TraCuuXeRaDto
        {
            MaVe = r.Get<long>("MaVe"),
            MaThe = r.Get<string>("MaThe") ?? "",
            BienSoVao = r.Get<string>("BienSoVao") ?? "",
            TenLoai = r.Get<string>("TenLoai") ?? "",
            MaKhuVuc = r.Get<string>("MaKhuVuc") ?? "",
            ThoiGianVao = r.Get<DateTime>("ThoiGianVao"),
            HinhAnhVao = r.Get<string>("HinhAnhVao"),
            TrangThai = r.Get<string>("TrangThai") ?? "",
            LoaiVe = r.Get<string>("LoaiVe") ?? "",
            KhungGui = r.Get<string>("KhungGui"),
            SoNgay = r.Get<int?>("SoNgay"),
            DonGia = r.Get<decimal?>("DonGia"),
            TongTienGoc = r.Get<decimal?>("TongTienGoc"),
            GhiChu = r.Get<string>("GhiChu"),
        };
    }

    /// <summary>Loại xe, loại thẻ của lượt và phí mất thẻ (bảng giá áp dụng tại ngày vào) — chỉ để hiển thị.</summary>
    public async Task<LuotGuiPhu> LayThongTinPhuAsync(long maVe)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var v = await db.VeGuiXes.AsNoTracking().Where(x => x.MaVe == maVe)
            .Select(x => new { x.MaXeNavigation.MaLoaiXe, x.MaTheNavigation.LoaiThe, x.ThoiGianVao })
            .FirstAsync();
        var ngay = DateOnly.FromDateTime(v.ThoiGianVao);
        var phi = await db.BangGia.AsNoTracking()
            .Where(b => b.MaLoaiXe == v.MaLoaiXe && b.NgayApDung <= ngay)
            .OrderByDescending(b => b.NgayApDung).Select(b => (decimal?)b.PhiMatVe).FirstOrDefaultAsync();
        return new LuotGuiPhu(v.MaLoaiXe, v.LoaiThe, phi ?? 0);
    }

    /// <summary>Khuyến mãi áp dụng được hôm nay cho loại xe/khung gửi (cùng điều kiện như sp_XuLyXeRa).</summary>
    public async Task<List<KhuyenMai>> LayKhuyenMaiApDungAsync(string maLoaiXe, string? khungGui)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.KhuyenMais.AsNoTracking()
            .Where(k => k.TrangThai == "Hoạt động" && k.NgayBatDau <= homNay && homNay <= k.NgayKetThuc
                        && (k.MaLoaiXe == null || k.MaLoaiXe == maLoaiXe)
                        && (k.KhungGuiApDung == null || k.KhungGuiApDung == khungGui))
            .OrderBy(k => k.MaKhuyenMai).ToListAsync();
    }

    public async Task<XeRaKetQua> XuLyXeRaAsync(string maNV, string bienSoRa, string? maThe, string? bienSoTimKiem,
        string? hinhAnhRa, string? maKhuyenMai, string phuongThucTT, bool xacNhanGiayTo, bool giuXe, string? ghiChu)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pHoaDon = DbProc.Out("@MaHoaDon", SqlDbType.BigInt);
        var pTong = DbProc.Out("@TongTien", SqlDbType.Decimal);
        var pKetQua = DbProc.Out("@KetQua", SqlDbType.NVarChar, 200);
        await DbProc.ExecuteAsync(db, "dbo.sp_XuLyXeRa",
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@BienSoRa", SqlDbType.NVarChar, bienSoRa, 50),
            DbProc.In("@MaThe", SqlDbType.VarChar, DbProc.NullNeuRong(maThe), 20),
            DbProc.In("@BienSoTimKiem", SqlDbType.NVarChar, DbProc.NullNeuRong(bienSoTimKiem), 50),
            DbProc.In("@HinhAnhRa", SqlDbType.NVarChar, DbProc.NullNeuRong(hinhAnhRa), 500),
            DbProc.In("@MaKhuyenMai", SqlDbType.VarChar, DbProc.NullNeuRong(maKhuyenMai), 10),
            DbProc.In("@PhuongThucTT", SqlDbType.NVarChar, phuongThucTT, 30),
            DbProc.In("@XacNhanGiayTo", SqlDbType.Bit, xacNhanGiayTo),
            DbProc.In("@GiuXe", SqlDbType.Bit, giuXe),
            DbProc.In("@GhiChu", SqlDbType.NVarChar, DbProc.NullNeuRong(ghiChu), 300),
            pHoaDon, pTong, pKetQua);
        return new XeRaKetQua(pHoaDon.GiaTri<long?>(), pTong.GiaTri<decimal?>() ?? 0, pKetQua.GiaTri<string>() ?? "");
    }
}

/// <summary>Đọc hóa đơn để in (HoaDon chỉ đọc).</summary>
public class HoaDonService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    public async Task<HoaDonInDto?> LayHoaDonInAsync(long maHoaDon)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.HoaDons.AsNoTracking().Where(h => h.MaHoaDon == maHoaDon)
            .Select(h => new HoaDonInDto
            {
                MaHoaDon = h.MaHoaDon,
                LoaiHoaDon = h.LoaiHoaDon,
                NgayTao = h.NgayTao,
                BienSo = h.MaVe != null ? h.MaVeNavigation.MaXeNavigation.BienSo : h.MaThanhVienNavigation.MaXeNavigation.BienSo,
                TenLoaiXe = h.MaBangGiaNavigation.MaLoaiXeNavigation.TenLoai,
                TenKhachHang = h.MaThanhVien != null ? h.MaThanhVienNavigation.MaKhNavigation.TenKh : null,
                MaThe = h.MaVe != null ? h.MaVeNavigation.MaThe : h.MaThanhVienNavigation.MaThe,
                ThoiGianVao = h.MaVe != null ? h.MaVeNavigation.ThoiGianVao : null,
                ThoiGianRa = h.MaVe != null ? h.MaVeNavigation.ThoiGianRa : null,
                KhungGui = h.KhungGui,
                SoNgay = h.SoNgay,
                SoThang = h.SoThang,
                NgayHetHan = h.MaThanhVien != null ? h.MaThanhVienNavigation.NgayHetHan : null,
                TongTienGoc = h.TongTienGoc,
                SoTienGiam = h.SoTienGiam,
                KhuyenMai = h.MaKhuyenMai != null ? h.MaKhuyenMai + " – " + h.MaKhuyenMaiNavigation.TenChuongTrinh : null,
                TienPhat = h.TienPhat,
                TongTien = h.TongTien,
                PhuongThucTT = h.PhuongThucTt,
                NhanVien = h.MaNvNavigation.TenNv + " (" + h.MaNv + ")",
            }).FirstOrDefaultAsync();
    }
}
