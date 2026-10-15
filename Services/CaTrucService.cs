using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Models;

namespace QLBGX.Services;

/// <summary>Số liệu phiên hiện tại (xem trước, tính đúng điều kiện như sp_ChotCa).</summary>
public record PhienHienTai(DateTime BatDau, DateTime Den, int LuotVao, int LuotRa, int SoHoaDon,
    decimal TienMat, decimal ChuyenKhoan, string? MaCa, string? TenCa);

public class CaTrucService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    public async Task<PhienHienTai> XemPhienHienTaiAsync(string maNV)
    {
        var now = DateTime.Now;
        await using var db = await dbFactory.CreateDbContextAsync();
        var nv = await db.NhanViens.AsNoTracking().Where(n => n.MaNv == maNV)
            .Select(n => new { n.NgayVaoLam, n.MaCa, TenCa = n.MaCaNavigation != null ? n.MaCaNavigation.TenCa : null })
            .FirstAsync();
        var lanChotCuoi = await db.LichSuCaTrucs.Where(l => l.MaNv == maNV).MaxAsync(l => (DateTime?)l.ThoiGianKetThuc);
        var batDau = lanChotCuoi ?? nv.NgayVaoLam.ToDateTime(TimeOnly.MinValue);

        var luotVao = await db.VeGuiXes.CountAsync(v => v.MaNv == maNV && v.ThoiGianVao > batDau && v.ThoiGianVao <= now);
        var luotRa = await db.VeGuiXes.CountAsync(v => v.MaNvra == maNV && v.ThoiGianRa > batDau && v.ThoiGianRa <= now);
        var hd = db.HoaDons.Where(h => h.MaNv == maNV && h.MaLichSuCa == null && h.NgayTao <= now);
        var soHd = await hd.CountAsync();
        var tienMat = await hd.Where(h => h.PhuongThucTt == "Tiền mặt").SumAsync(h => (decimal?)h.TongTien) ?? 0;
        var ck = await hd.Where(h => h.PhuongThucTt == "Chuyển khoản").SumAsync(h => (decimal?)h.TongTien) ?? 0;
        return new PhienHienTai(batDau, now, luotVao, luotRa, soHd, tienMat, ck, nv.MaCa, nv.TenCa);
    }

    /// <summary>sp_ChotCa → trả dòng LichSuCaTruc vừa tạo.</summary>
    public async Task<LichSuCaTruc> ChotCaAsync(string maNV, decimal tienMatThucNop, string? ghiChu)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pMa = DbProc.Out("@MaLichSuCa", SqlDbType.Int);
        await DbProc.ExecuteAsync(db, "dbo.sp_ChotCa",
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            new Microsoft.Data.SqlClient.SqlParameter("@TienMatThucNop", SqlDbType.Decimal)
                { Precision = 18, Scale = 2, Value = tienMatThucNop },
            DbProc.In("@GhiChu", SqlDbType.NVarChar, DbProc.NullNeuRong(ghiChu), 300),
            pMa);
        var ma = pMa.GiaTri<int>();
        return await db.LichSuCaTrucs.AsNoTracking().Include(l => l.MaCaNavigation).Include(l => l.MaNvNavigation)
            .FirstAsync(l => l.MaLichSuCa == ma);
    }

    public async Task<LichSuCaTruc?> LayMotPhienAsync(int maLichSuCa)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.LichSuCaTrucs.AsNoTracking().Include(l => l.MaCaNavigation).Include(l => l.MaNvNavigation)
            .FirstOrDefaultAsync(l => l.MaLichSuCa == maLichSuCa);
    }

    public async Task<List<VwDoanhThuTheoCa>> LayPhienAsync(string? chiCuaMaNV = null, bool chiChoXacNhan = false,
        DateOnly? tu = null, DateOnly? den = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.VwDoanhThuTheoCas.AsNoTracking();
        if (chiCuaMaNV != null) q = q.Where(x => x.MaNv == chiCuaMaNV);
        if (chiChoXacNhan) q = q.Where(x => x.TrangThai == "Chờ xác nhận");
        if (tu != null) q = q.Where(x => x.NgayCa >= tu);
        if (den != null) q = q.Where(x => x.NgayCa <= den);
        return await q.OrderByDescending(x => x.ThoiGianKetThuc).ToListAsync();
    }

    public async Task XacNhanChotCaAsync(int maLichSuCa, string maNVQuanLy, string? ghiChu)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_XacNhanChotCa",
            DbProc.In("@MaLichSuCa", SqlDbType.Int, maLichSuCa),
            DbProc.In("@MaNVQuanLy", SqlDbType.VarChar, maNVQuanLy, 10),
            DbProc.In("@GhiChu", SqlDbType.NVarChar, DbProc.NullNeuRong(ghiChu), 300));
    }

    /// <summary>sp_CapNhatCaTruc; maCa = null nghĩa là bỏ ca.</summary>
    public async Task CapNhatCaTrucAsync(string maNV, string? maCa, string maNVQuanLy)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_CapNhatCaTruc",
            DbProc.In("@MaNV", SqlDbType.VarChar, maNV, 10),
            DbProc.In("@MaCa", SqlDbType.VarChar, DbProc.NullNeuRong(maCa), 10),
            DbProc.In("@MaNVQuanLy", SqlDbType.VarChar, maNVQuanLy, 10));
    }
}
