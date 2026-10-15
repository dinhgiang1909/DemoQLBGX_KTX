using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Helpers;
using QLBGX.Models;

namespace QLBGX.Services;

public class TheRow
{
    public string MaThe { get; init; } = "";
    public string LoaiThe { get; init; } = "";
    public string TrangThai { get; init; } = "";
    public DateOnly NgayKhaiBao { get; init; }
    public string? GhiChu { get; init; }
    public string GanVoi { get; init; } = "";
}

public class TheService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    /// <summary>Giá trị lọc đặc biệt: thẻ "Đang sử dụng" mà không gắn lượt gửi trong bãi hay vé tháng nào.</summary>
    public const string LocLech = "Lệch";

    /// <summary>Thẻ "Đang sử dụng" nhưng không có lượt gửi trong bãi và không gắn vé tháng (dữ liệu lệch, mục 3.1.6).</summary>
    public static IQueryable<The> LocTheLech(QuanLyBaiGiuXeKTXContext db, IQueryable<The> q) =>
        q.Where(t => t.TrangThai == "Đang sử dụng"
                     && !db.VeGuiXes.Any(v => v.MaThe == t.MaThe && (v.TrangThai == "Đang gửi" || v.TrangThai == "Chờ xử lý"))
                     && !db.ThanhViens.Any(tv => tv.MaThe == t.MaThe));

    public async Task<List<TheRow>> LayDanhSachAsync(string? loaiThe, string? trangThai, string? tuKhoa)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Thes.AsNoTracking();
        if (!string.IsNullOrEmpty(loaiThe)) q = q.Where(t => t.LoaiThe == loaiThe);
        if (trangThai == LocLech) q = LocTheLech(db, q);
        else if (!string.IsNullOrEmpty(trangThai)) q = q.Where(t => t.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(t => t.MaThe.Contains(tuKhoa.Trim()));

        var ds = await q.OrderBy(t => t.MaThe).Select(t => new
        {
            t.MaThe, t.LoaiThe, t.TrangThai, t.NgayKhaiBao, t.GhiChu,
            Luot = db.VeGuiXes.Where(v => v.MaThe == t.MaThe && (v.TrangThai == "Đang gửi" || v.TrangThai == "Chờ xử lý"))
                .Select(v => "Lượt #" + v.MaVe + " – xe " + v.MaXeNavigation.BienSo + " (" + v.TrangThai + ")").FirstOrDefault(),
            VeThang = db.ThanhViens.Where(tv => tv.MaThe == t.MaThe)
                .Select(tv => "Vé tháng #" + tv.MaThanhVien + " – " + tv.MaKhNavigation.TenKh + " – xe " + tv.MaXeNavigation.BienSo)
                .FirstOrDefault(),
        }).ToListAsync();

        return ds.Select(x => new TheRow
        {
            MaThe = x.MaThe, LoaiThe = x.LoaiThe, TrangThai = x.TrangThai, NgayKhaiBao = x.NgayKhaiBao, GhiChu = x.GhiChu,
            GanVoi = x.TrangThai == "Đang sử dụng" && x.VeThang == null && x.Luot == null
                ? "⚠ Đang sử dụng nhưng không có lượt gửi/vé tháng – dữ liệu lệch, Quản lý kiểm tra"
                : string.Join("; ", new[] { x.VeThang, x.Luot }.Where(s => s != null)),
        }).ToList();
    }

    /// <summary>Số thẻ lượt Rảnh còn trong kho và số thẻ lệch dữ liệu (toàn kho, không theo bộ lọc).</summary>
    public async Task<(int TheLuotRanh, int TheLech)> DemCanhBaoAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ranh = await db.Thes.CountAsync(t => t.LoaiThe == "Thẻ lượt" && t.TrangThai == "Rảnh");
        var lech = await LocTheLech(db, db.Thes).CountAsync();
        return (ranh, lech);
    }

    public async Task KhaiBaoAsync(string maThe, string loaiThe, string maNVQuanLy)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_KhaiBaoThe",
            DbProc.In("@MaThe", SqlDbType.VarChar, maThe, 20),
            DbProc.In("@LoaiThe", SqlDbType.NVarChar, loaiThe, 20),
            DbProc.In("@MaNVQuanLy", SqlDbType.VarChar, maNVQuanLy, 10));
    }

    /// <summary>
    /// Khóa thẻ thủ công (P1.7, chỉ Quản lý): thẻ hỏng/thất lạc, hoặc thẻ "Đang sử dụng" mà không có lượt gửi (dữ liệu lệch).
    /// CSDL chưa có thủ tục riêng nên cập nhật một bảng THE bằng một câu lệnh có điều kiện:
    /// thẻ gắn vé tháng → khóa vé tháng/cấp lại thẻ; thẻ có lượt trong bãi → xử lý mất thẻ ở màn hình Xe ra.
    /// </summary>
    public async Task KhoaAsync(string maThe, string maNVQuanLy, string lyDo)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.NhanViens.AnyAsync(n => n.MaNv == maNVQuanLy && n.ChucVu == "Quản lý" && n.TrangThai == "Đang làm"))
            throw new LoiNghiepVuException("Chỉ Quản lý được khóa thẻ.");

        var the = await db.Thes.AsNoTracking().FirstOrDefaultAsync(t => t.MaThe == maThe)
                  ?? throw new LoiNghiepVuException("Thẻ không tồn tại.");
        if (the.TrangThai == "Khóa")
            throw new LoiNghiepVuException("Thẻ đã ở trạng thái Khóa.");
        if (await db.ThanhViens.AnyAsync(tv => tv.MaThe == maThe))
            throw new LoiNghiepVuException("Thẻ đang gắn với vé tháng: dùng Khóa vé tháng hoặc Cấp lại thẻ ở màn hình Khách hàng & Vé tháng.");

        var ghiChu = $"Khóa thủ công {DateTime.Today:dd/MM/yyyy}: {lyDo}";
        if (ghiChu.Length > 200) ghiChu = ghiChu[..200];
        var n = await db.Thes
            .Where(t => t.MaThe == maThe && t.TrangThai != "Khóa"
                        && !db.VeGuiXes.Any(v => v.MaThe == maThe && (v.TrangThai == "Đang gửi" || v.TrangThai == "Chờ xử lý"))
                        && !db.ThanhViens.Any(tv => tv.MaThe == maThe))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.TrangThai, "Khóa").SetProperty(t => t.GhiChu, ghiChu));
        if (n == 0)
            throw new LoiNghiepVuException("Thẻ đang gắn với lượt gửi trong bãi: xử lý \"Khách mất thẻ\" ở màn hình Xe ra (hệ thống tự khóa thẻ).");
    }

    public async Task MoKhoaAsync(string maThe, string maNVQuanLy)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_MoKhoaThe",
            DbProc.In("@MaThe", SqlDbType.VarChar, maThe, 20),
            DbProc.In("@MaNVQuanLy", SqlDbType.VarChar, maNVQuanLy, 10));
    }
}
