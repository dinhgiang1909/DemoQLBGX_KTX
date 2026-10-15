using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Models;
using QLBGX.Models.Dtos;

namespace QLBGX.Services;

public record DashboardSoLieu(int LuotVao, int LuotRa, decimal DoanhThuTienMat, decimal DoanhThuChuyenKhoan,
    int TheLuotRanh, int XeTrongBai, int XeTonDong, int ChoXuLy, int ChotCaChoXacNhan, int VeThangSapHetHan,
    int TheLech);

public record LuotChoXuLy(long MaVe, string BienSo, string? BienSoRa, string MaThe, DateTime ThoiGianVao, string? GhiChu);

public class BaoCaoService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    public async Task<BaoCaoDoanhThuDto> BaoCaoDoanhThuAsync(DateOnly tu, DateOnly den)
    {
        var kq = new BaoCaoDoanhThuDto();
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var cmd = await DbProc.TaoLenhAsync(db, "dbo.sp_BaoCaoDoanhThu",
            DbProc.In("@TuNgay", SqlDbType.Date, tu.ToDateTime(TimeOnly.MinValue)),
            DbProc.In("@DenNgay", SqlDbType.Date, den.ToDateTime(TimeOnly.MinValue)));
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            kq.ChiTiet.Add(new DoanhThuChiTietDto
            {
                Ngay = r.Get<DateOnly>("Ngay"),
                LoaiHoaDon = r.Get<string>("LoaiHoaDon") ?? "",
                MaLoaiXe = r.Get<string>("MaLoaiXe"),
                KhungGui = r.Get<string>("KhungGui"),
                PhuongThucTT = r.Get<string>("PhuongThucTT") ?? "",
                SoHoaDon = r.Get<int>("SoHoaDon"),
                TongTienGoc = r.Get<decimal>("TongTienGoc"),
                TongGiam = r.Get<decimal>("TongGiam"),
                TongPhat = r.Get<decimal>("TongPhat"),
                DoanhThu = r.Get<decimal>("DoanhThu"),
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            kq.TheoLoaiXe.Add(new DoanhThuNhomDto
            {
                MaLoaiXe = r.Get<string>("MaLoaiXe"),
                LoaiHoaDon = r.Get<string>("LoaiHoaDon") ?? "",
                KhungGui = r.Get<string>("KhungGui"),
                SoHoaDon = r.Get<int>("SoHoaDon"),
                DoanhThu = r.Get<decimal>("DoanhThu"),
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            kq.TheoPhuongThuc.Add(new DoanhThuPhuongThucDto
            {
                PhuongThucTT = r.Get<string>("PhuongThucTT") ?? "",
                SoHoaDon = r.Get<int>("SoHoaDon"),
                DoanhThu = r.Get<decimal>("DoanhThu"),
            });
        return kq;
    }

    public async Task<List<VwTrangThaiKhuVuc>> TrangThaiKhuVucAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwTrangThaiKhuVucs.AsNoTracking().OrderBy(x => x.MaKhuVuc).ToListAsync();
    }

    public async Task<List<VwXeTonDong>> XeTonDongAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwXeTonDongs.AsNoTracking().OrderByDescending(x => x.SoNgayDaGui).ToListAsync();
    }

    public async Task<List<VwLuotGuiLechBienSo>> LechBienSoAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwLuotGuiLechBienSos.AsNoTracking().OrderByDescending(x => x.ThoiGianVao).ToListAsync();
    }

    public async Task<List<VwVeThangSapHetHan>> VeThangSapHetHanAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwVeThangSapHetHans.AsNoTracking().OrderBy(x => x.NgayHetHan).ToListAsync();
    }

    public async Task<List<LuotChoXuLy>> LuotChoXuLyAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VeGuiXes.AsNoTracking().Where(v => v.TrangThai == "Chờ xử lý")
            .OrderBy(v => v.ThoiGianVao)
            .Select(v => new LuotChoXuLy(v.MaVe, v.MaXeNavigation.BienSo, v.BienSoRa, v.MaThe, v.ThoiGianVao, v.GhiChu))
            .ToListAsync();
    }

    public async Task<DashboardSoLieu> DashboardAsync(DateOnly ngay)
    {
        var a = ngay.ToDateTime(TimeOnly.MinValue);
        var b = a.AddDays(1);
        await using var db = await dbFactory.CreateDbContextAsync();
        var luotVao = await db.VeGuiXes.CountAsync(v => v.ThoiGianVao >= a && v.ThoiGianVao < b);
        var luotRa = await db.VeGuiXes.CountAsync(v => v.ThoiGianRa >= a && v.ThoiGianRa < b);
        var hd = db.HoaDons.Where(h => h.NgayTao >= a && h.NgayTao < b);
        var tienMat = await hd.Where(h => h.PhuongThucTt == "Tiền mặt").SumAsync(h => (decimal?)h.TongTien) ?? 0;
        var ck = await hd.Where(h => h.PhuongThucTt == "Chuyển khoản").SumAsync(h => (decimal?)h.TongTien) ?? 0;
        var theRanh = await db.Thes.CountAsync(t => t.LoaiThe == "Thẻ lượt" && t.TrangThai == "Rảnh");
        var trongBai = await db.VeGuiXes.CountAsync(v => v.TrangThai == "Đang gửi" || v.TrangThai == "Chờ xử lý");
        var tonDong = await db.VwXeTonDongs.CountAsync();
        var choXuLy = await db.VeGuiXes.CountAsync(v => v.TrangThai == "Chờ xử lý");
        var chotCa = await db.LichSuCaTrucs.CountAsync(l => l.TrangThai == "Chờ xác nhận");
        var veThang = await db.VwVeThangSapHetHans.CountAsync();
        var theLech = await TheService.LocTheLech(db, db.Thes).CountAsync();
        return new DashboardSoLieu(luotVao, luotRa, tienMat, ck, theRanh, trongBai, tonDong, choXuLy, chotCa, veThang, theLech);
    }
}
