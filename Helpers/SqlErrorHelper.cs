using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace QLBGX.Helpers;

/// <summary>Chuyển lỗi CSDL thành thông điệp cho người dùng.</summary>
public static class SqlErrorHelper
{
    /// <summary>Lỗi nghiệp vụ do thủ tục THROW (mã 50000 trở lên).</summary>
    public static bool LaLoiNghiepVu(Exception ex) =>
        ex is LoiNghiepVuException || TimSqlException(ex) is { Number: >= 50000 };

    public static string ThongDiep(Exception ex)
    {
        if (ex is LoiNghiepVuException) return ex.Message;
        var sql = TimSqlException(ex);
        if (sql is { Number: >= 50000 })
            return sql.Message;   // thông điệp tiếng Việt từ CSDL

        Debug.WriteLine(ex);
        var chiTiet = sql != null ? $"[{sql.Number}] {sql.Message}" : ex.GetBaseException().Message;
        return $"Lỗi hệ thống: {chiTiet}";
    }

    private static SqlException? TimSqlException(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is SqlException s) return s;
            ex = ex.InnerException;
        }
        return null;
    }
}
