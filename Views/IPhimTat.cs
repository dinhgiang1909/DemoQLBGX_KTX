using System.Windows.Input;

namespace QLBGX.Views;

/// <summary>Trang có phím tắt riêng (F2/F5/Esc). Cửa sổ chính chuyển phím cho trang đang chứa con trỏ.</summary>
public interface IPhimTat
{
    /// <returns>true nếu trang đã xử lý phím.</returns>
    bool XuLyPhim(Key phim);
}
