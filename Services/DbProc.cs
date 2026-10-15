using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QLBGX.Services;

/// <summary>Gọi thủ tục lưu trữ bằng ADO.NET qua kết nối của EF.</summary>
public static class DbProc
{
    public static async Task<DbCommand> TaoLenhAsync(DbContext db, string tenThuTuc, params SqlParameter[] thamSo)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = tenThuTuc;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddRange(thamSo);
        return cmd;
    }

    public static async Task ExecuteAsync(DbContext db, string tenThuTuc, params SqlParameter[] thamSo)
    {
        await using var cmd = await TaoLenhAsync(db, tenThuTuc, thamSo);
        await cmd.ExecuteNonQueryAsync();
    }

    public static SqlParameter In(string ten, SqlDbType kieu, object? giaTri, int size = 0)
    {
        var p = new SqlParameter(ten, kieu) { Value = giaTri ?? DBNull.Value };
        if (size > 0) p.Size = size;
        return p;
    }

    public static SqlParameter Out(string ten, SqlDbType kieu, int size = 0)
    {
        var p = new SqlParameter(ten, kieu) { Direction = ParameterDirection.Output };
        if (size > 0) p.Size = size;
        if (kieu == SqlDbType.Decimal)
        {
            p.Precision = 18;
            p.Scale = 2;
        }
        return p;
    }

    /// <summary>Chuỗi rỗng/khoảng trắng → NULL.</summary>
    public static object? NullNeuRong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static T? GiaTri<T>(this SqlParameter p) => p.Value is null or DBNull ? default : (T)p.Value;

    public static T? Get<T>(this DbDataReader r, string cot)
    {
        var i = r.GetOrdinal(cot);
        if (r.IsDBNull(i)) return default;
        var v = r.GetValue(i);
        var dich = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (dich == typeof(DateOnly) && v is DateTime dt) return (T)(object)DateOnly.FromDateTime(dt);
        return v is T t ? t : (T)Convert.ChangeType(v, dich);
    }
}
