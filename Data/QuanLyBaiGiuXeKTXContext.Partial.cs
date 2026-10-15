using Microsoft.EntityFrameworkCore;

namespace QLBGX.Models;

/// <summary>
/// Cấu hình bổ sung cho context do EF Core Power Tools sinh ra (không sửa file trong Models/).
/// Đã kiểm tra: context sinh tự động đã khai báo HasTrigger cho đủ các trigger trong script SQL
/// (VeGuiXe, ThanhVien, HoaDon ×2, NhanVien, KhachHang, KhuyenMai, LoaiXe, KhuVucDo, THE),
/// nên ở đây không cần thêm. Nếu sinh lại model mà mất cấu hình, bổ sung trong hàm dưới.
/// </summary>
public partial class QuanLyBaiGiuXeKTXContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
    }
}
