using System.Data;
using Microsoft.EntityFrameworkCore;
using QLBGX.Models;
using QLBGX.Models.Dtos;

namespace QLBGX.Services;

/// <summary>Đăng nhập (sp_DangNhap) và đổi mật khẩu (sp_DoiMatKhau).</summary>
public class AuthService(IDbContextFactory<QuanLyBaiGiuXeKTXContext> dbFactory)
{
    /// <returns>null nếu sai tài khoản/mật khẩu hoặc đã nghỉ việc.</returns>
    public async Task<DangNhapDto?> DangNhapAsync(string taiKhoan, string matKhau)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var cmd = await DbProc.TaoLenhAsync(db, "dbo.sp_DangNhap",
            DbProc.In("@TaiKhoan", SqlDbType.VarChar, taiKhoan, 50),
            DbProc.In("@MatKhau", SqlDbType.VarChar, matKhau, 100));
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new DangNhapDto(
            r.Get<string>("MaNV")!,
            r.Get<string>("TenNV")!,
            r.Get<string>("ChucVu")!,
            r.Get<string>("MaCa"),
            r.Get<string>("TenCa"));
    }

    /// <summary>Ném SqlException 50015 (mật khẩu mới &lt; 6 ký tự) hoặc 50016 (sai tài khoản/mật khẩu cũ).</summary>
    public async Task DoiMatKhauAsync(string taiKhoan, string matKhauCu, string matKhauMoi)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await DbProc.ExecuteAsync(db, "dbo.sp_DoiMatKhau",
            DbProc.In("@TaiKhoan", SqlDbType.VarChar, taiKhoan, 50),
            DbProc.In("@MatKhauCu", SqlDbType.VarChar, matKhauCu, 100),
            DbProc.In("@MatKhauMoi", SqlDbType.VarChar, matKhauMoi, 100));
    }
}
