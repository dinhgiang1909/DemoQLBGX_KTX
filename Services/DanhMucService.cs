using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QLBGX.Helpers;
using QLBGX.Models;

namespace QLBGX.Services;

/// <summary>
/// Danh mục đơn giản đọc/ghi bằng EF Core. Không gọi Remove() với các bảng có trigger xóa mềm:
/// "Ngừng/Xóa" chỉ đổi cột TrangThai.
/// </summary>
public class DanhMucService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    // ---------- Loại xe ----------
    public async Task<List<LoaiXe>> LayLoaiXeAsync(bool chiHoatDong = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.LoaiXes.AsNoTracking();
        if (chiHoatDong) q = q.Where(x => x.TrangThai == "Hoạt động");
        return await q.OrderBy(x => x.MaLoaiXe).ToListAsync();
    }

    public async Task LuuLoaiXeAsync(LoaiXe lx, bool themMoi)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (themMoi)
        {
            if (await db.LoaiXes.AnyAsync(x => x.MaLoaiXe == lx.MaLoaiXe))
                throw new LoiNghiepVuException($"Mã loại xe {lx.MaLoaiXe} đã tồn tại.");
            db.LoaiXes.Add(lx);
        }
        else
        {
            var e = await db.LoaiXes.FirstAsync(x => x.MaLoaiXe == lx.MaLoaiXe);
            e.TenLoai = lx.TenLoai;
            e.MoTa = lx.MoTa;
            e.TrangThai = lx.TrangThai;
        }
        await db.SaveChangesAsync();
    }

    // ---------- Khu vực đỗ ----------
    public async Task<List<KhuVucDo>> LayKhuVucAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.KhuVucDos.AsNoTracking().OrderBy(x => x.MaKhuVuc).ToListAsync();
    }

    public async Task<Dictionary<string, int>> LaySoXeDangGuiAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.VwTrangThaiKhuVucs.AsNoTracking().ToDictionaryAsync(x => x.MaKhuVuc, x => x.SoXeDangGui ?? 0);
    }

    public async Task LuuKhuVucAsync(KhuVucDo kv, bool themMoi)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (themMoi)
        {
            if (await db.KhuVucDos.AnyAsync(x => x.MaKhuVuc == kv.MaKhuVuc))
                throw new LoiNghiepVuException($"Mã khu vực {kv.MaKhuVuc} đã tồn tại.");
            db.KhuVucDos.Add(kv);
        }
        else
        {
            var soXe = await db.VwTrangThaiKhuVucs.Where(x => x.MaKhuVuc == kv.MaKhuVuc)
                .Select(x => x.SoXeDangGui).FirstOrDefaultAsync() ?? 0;
            if (kv.SucChua < soXe)
                throw new LoiNghiepVuException($"Không thể giảm sức chứa xuống {kv.SucChua}: khu vực đang có {soXe} xe.");
            var e = await db.KhuVucDos.FirstAsync(x => x.MaKhuVuc == kv.MaKhuVuc);
            if (e.MaLoaiXe != kv.MaLoaiXe && soXe > 0)
                throw new LoiNghiepVuException("Khu vực đang có xe, không thể đổi loại xe.");
            e.TenKhuVuc = kv.TenKhuVuc;
            e.SucChua = kv.SucChua;
            e.MaLoaiXe = kv.MaLoaiXe;
            e.TrangThai = kv.TrangThai;
            e.GhiChu = kv.GhiChu;
        }
        await db.SaveChangesAsync();
    }

    // ---------- Bảng giá (chỉ thêm mới) ----------
    public async Task<List<BangGium>> LayBangGiaAsync(string? maLoaiXe)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.BangGia.AsNoTracking();
        if (!string.IsNullOrEmpty(maLoaiXe)) q = q.Where(x => x.MaLoaiXe == maLoaiXe);
        return await q.OrderBy(x => x.MaLoaiXe).ThenByDescending(x => x.NgayApDung).ToListAsync();
    }

    public async Task<string> TaoMaBangGiaMoiAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ma = await db.BangGia.Select(x => x.MaBangGia).ToListAsync();
        var max = ma.Select(m => m.StartsWith("BG") && int.TryParse(m[2..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"BG{max + 1:00}";
    }

    public async Task ThemBangGiaAsync(BangGium bg)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.BangGia.AnyAsync(x => x.MaBangGia == bg.MaBangGia))
            throw new LoiNghiepVuException($"Mã bảng giá {bg.MaBangGia} đã tồn tại.");
        if (await db.BangGia.AnyAsync(x => x.MaLoaiXe == bg.MaLoaiXe && x.NgayApDung == bg.NgayApDung))
            throw new LoiNghiepVuException("Loại xe này đã có bảng giá áp dụng từ ngày đó.");
        db.BangGia.Add(bg);
        await db.SaveChangesAsync();
    }

    // ---------- Khuyến mãi ----------
    public async Task<List<KhuyenMai>> LayKhuyenMaiAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.KhuyenMais.AsNoTracking().OrderByDescending(x => x.NgayBatDau).ToListAsync();
    }

    public async Task LuuKhuyenMaiAsync(KhuyenMai km, bool themMoi)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (themMoi)
        {
            if (await db.KhuyenMais.AnyAsync(x => x.MaKhuyenMai == km.MaKhuyenMai))
                throw new LoiNghiepVuException($"Mã khuyến mãi {km.MaKhuyenMai} đã tồn tại.");
            db.KhuyenMais.Add(km);
        }
        else
        {
            var e = await db.KhuyenMais.FirstAsync(x => x.MaKhuyenMai == km.MaKhuyenMai);
            e.TenChuongTrinh = km.TenChuongTrinh;
            e.PhanTramGiam = km.PhanTramGiam;
            e.DieuKienApDung = km.DieuKienApDung;
            e.MaLoaiXe = km.MaLoaiXe;
            e.KhungGuiApDung = km.KhungGuiApDung;
            e.NgayBatDau = km.NgayBatDau;
            e.NgayKetThuc = km.NgayKetThuc;
            e.TrangThai = km.TrangThai;
        }
        await db.SaveChangesAsync();
    }

    // ---------- Nhân viên ----------
    /// <summary>Băm giống CSDL: CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', @MatKhau VARCHAR), 2).</summary>
    public static string BamMatKhau(string matKhau) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(matKhau)));

    public async Task<List<NhanVien>> LayNhanVienAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.NhanViens.AsNoTracking().Include(x => x.MaCaNavigation)
            .OrderBy(x => x.TrangThai != "Đang làm").ThenBy(x => x.MaNv).ToListAsync();
    }

    public async Task ThemNhanVienAsync(NhanVien nv, string matKhau)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.NhanViens.AnyAsync(x => x.MaNv == nv.MaNv))
            throw new LoiNghiepVuException($"Mã nhân viên {nv.MaNv} đã tồn tại.");
        if (await db.NhanViens.AnyAsync(x => x.TaiKhoan == nv.TaiKhoan))
            throw new LoiNghiepVuException($"Tài khoản {nv.TaiKhoan} đã tồn tại.");
        nv.MatKhau = BamMatKhau(matKhau);
        nv.TrangThai = "Đang làm";
        db.NhanViens.Add(nv);
        await db.SaveChangesAsync();
    }

    /// <summary>Sửa thông tin (không đổi ca — đổi ca dùng sp_CapNhatCaTruc). matKhauMoi khác rỗng thì đặt lại mật khẩu.</summary>
    public async Task SuaNhanVienAsync(NhanVien nv, string? matKhauMoi)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.NhanViens.AnyAsync(x => x.TaiKhoan == nv.TaiKhoan && x.MaNv != nv.MaNv))
            throw new LoiNghiepVuException($"Tài khoản {nv.TaiKhoan} đã tồn tại.");
        var e = await db.NhanViens.FirstAsync(x => x.MaNv == nv.MaNv);
        e.TenNv = nv.TenNv;
        e.ChucVu = nv.ChucVu;
        e.Sdt = nv.Sdt;
        e.NgayVaoLam = nv.NgayVaoLam;
        e.TaiKhoan = nv.TaiKhoan;
        if (!string.IsNullOrEmpty(matKhauMoi)) e.MatKhau = BamMatKhau(matKhauMoi);
        await db.SaveChangesAsync();
    }

    /// <summary>Cho nghỉ việc: đổi TrangThai (ca đã được bỏ trước đó qua sp_CapNhatCaTruc).</summary>
    public async Task ChoNghiViecAsync(string maNV)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.NhanViens.FirstAsync(x => x.MaNv == maNV);
        e.TrangThai = "Nghỉ việc";
        e.MaCa = null;
        await db.SaveChangesAsync();
    }

    // ---------- Ca làm việc ----------
    public async Task<List<CaLamViec>> LayCaAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.CaLamViecs.AsNoTracking().OrderBy(x => x.MaCa).ToListAsync();
    }

    public async Task LuuCaAsync(CaLamViec ca)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.CaLamViecs.FirstAsync(x => x.MaCa == ca.MaCa);
        e.TenCa = ca.TenCa;
        e.GioBatDau = ca.GioBatDau;
        e.GioKetThuc = ca.GioKetThuc;
        await db.SaveChangesAsync();
    }
}
