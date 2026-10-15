using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Models.Dtos;

namespace QLBGX.Services;

/// <summary>Thông tin vé tháng để hiển thị (xe vào, tra cứu biển số).</summary>
public record VeThangInfo(int MaThanhVien, string MaThe, string TenKH, string MaDinhDanh, string BienSo,
    string MaLoaiXe, string TenLoai, DateOnly NgayHetHan, string TrangThai);

/// <summary>Kết quả tra cứu nhanh theo biển số (chỉ để gợi ý cho nhân viên).</summary>
public record TraCuuBienSoInfo(bool DaCoXe, string? MaLoaiXe, string? TenLoai, long? MaVeTrongBai, VeThangInfo? VeThang);

/// <summary>Thông tin thẻ vừa quẹt.</summary>
public record TheInfo(string MaThe, string LoaiThe, string TrangThai, VeThangInfo? VeThang, bool CoLuotTrongBai);

public record LuotVaoGanDay(long MaVe, DateTime ThoiGianVao, string BienSo, string TenLoai, string MaThe,
    string MaKhuVuc, string LoaiThe, string TrangThai);

public class XeVaoService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    public static readonly string[] TrongBai = ["Đang gửi", "Chờ xử lý"];

    public async Task<List<LoaiXe>> LayLoaiXeHoatDongAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.LoaiXes.AsNoTracking().Where(l => l.TrangThai == "Hoạt động").OrderBy(l => l.TenLoai).ToListAsync();
    }

    private static IQueryable<VeThangInfo> ChonVeThang(IQueryable<ThanhVien> q) =>
        q.Select(tv => new VeThangInfo(tv.MaThanhVien, tv.MaThe, tv.MaKhNavigation.TenKh, tv.MaKhNavigation.MaDinhDanh,
            tv.MaXeNavigation.BienSo, tv.MaXeNavigation.MaLoaiXe, tv.MaXeNavigation.MaLoaiXeNavigation.TenLoai,
            tv.NgayHetHan, tv.TrangThai));

    public async Task<TraCuuBienSoInfo> TraCuuBienSoAsync(string bienSo)
    {
        var bs = BienSoHelper.ChuanHoa(bienSo);
        await using var db = await dbFactory.CreateDbContextAsync();
        var xe = await db.Xes.AsNoTracking().Where(x => x.BienSo == bs)
            .Select(x => new { x.MaXe, x.MaLoaiXe, x.MaLoaiXeNavigation.TenLoai }).FirstOrDefaultAsync();
        if (xe == null) return new TraCuuBienSoInfo(false, null, null, null, null);

        var maVe = await db.VeGuiXes.AsNoTracking()
            .Where(v => v.MaXe == xe.MaXe && TrongBai.Contains(v.TrangThai))
            .Select(v => (long?)v.MaVe).FirstOrDefaultAsync();
        var veThang = await ChonVeThang(db.ThanhViens.AsNoTracking().Where(tv => tv.MaXe == xe.MaXe)).FirstOrDefaultAsync();
        return new TraCuuBienSoInfo(true, xe.MaLoaiXe, xe.TenLoai, maVe, veThang);
    }

    public async Task<TheInfo?> TraCuuTheAsync(string maThe)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var the = await db.Thes.AsNoTracking().FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the == null) return null;
        var veThang = the.LoaiThe == "Thẻ tháng"
            ? await ChonVeThang(db.ThanhViens.AsNoTracking().Where(tv => tv.MaThe == maThe)).FirstOrDefaultAsync()
            : null;
        var coLuot = await db.VeGuiXes.AnyAsync(v => v.MaThe == maThe && TrongBai.Contains(v.TrangThai));
        return new TheInfo(the.MaThe, the.LoaiThe, the.TrangThai, veThang, coLuot);
    }

    public async Task<int> DemTheLuotRanhAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Thes.CountAsync(t => t.LoaiThe == "Thẻ lượt" && t.TrangThai == "Rảnh");
    }

    /// <summary>Bảng giá có NgayApDung gần nhất ≤ ngày (giống fn_LayBangGia).</summary>
    public async Task<BangGium?> LayBangGiaAsync(string maLoaiXe, DateOnly ngay)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BangGia.AsNoTracking()
            .Where(b => b.MaLoaiXe == maLoaiXe && b.NgayApDung <= ngay)
            .OrderByDescending(b => b.NgayApDung).FirstOrDefaultAsync();
    }

    public async Task<XeVaoKetQua> XuLyXeVaoAsync(string bienSo, string maLoaiXe, string maThe, string maNV,
        string? hinhAnhVao, string? ghiChu)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pMaVe = DbProc.Out("@MaVe", SqlDbType.BigInt);
        var pKhuVuc = DbProc.Out("@MaKhuVuc", SqlDbType.VarChar, 10);
        var pLoaiVe = DbProc.Out("@LoaiVe", SqlDbType.NVarChar, 20);
        await DbProc.ExecuteAsync(db, "dbo.sp_XuLyXeVao",
            DbProc.In("@BienSo", SqlDbType.NVarChar, bienSo, 50),
            DbProc.In("@MaLoaiXe", SqlDbType.VarChar, maLoaiXe, 10),
            DbProc.In("@MaThe", SqlDbType.VarChar, maThe, 20),
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@HinhAnhVao", SqlDbType.NVarChar, DbProc.NullNeuRong(hinhAnhVao), 500),
            DbProc.In("@GhiChu", SqlDbType.NVarChar, DbProc.NullNeuRong(ghiChu), 300),
            pMaVe, pKhuVuc, pLoaiVe);
        return new XeVaoKetQua(pMaVe.GiaTri<long>(), pKhuVuc.GiaTri<string>() ?? "", pLoaiVe.GiaTri<string>() ?? "");
    }

    public async Task<List<LuotVaoGanDay>> LayLuotVaoGanNhatAsync(int soLuong = 10)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VeGuiXes.AsNoTracking()
            .OrderByDescending(v => v.ThoiGianVao).ThenByDescending(v => v.MaVe)
            .Take(soLuong)
            .Select(v => new LuotVaoGanDay(v.MaVe, v.ThoiGianVao, v.MaXeNavigation.BienSo,
                v.MaXeNavigation.MaLoaiXeNavigation.TenLoai, v.MaThe, v.MaKhuVuc, v.MaTheNavigation.LoaiThe, v.TrangThai))
            .ToListAsync();
    }

    public async Task<List<VwTrangThaiKhuVuc>> LayTrangThaiKhuVucAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwTrangThaiKhuVucs.AsNoTracking().OrderBy(k => k.MaKhuVuc).ToListAsync();
    }
}
