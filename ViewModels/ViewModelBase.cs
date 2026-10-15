using CommunityToolkit.Mvvm.ComponentModel;
using QLBGX.Services;

namespace QLBGX.ViewModels;

/// <summary>Cơ sở cho ViewModel của các trang: cờ "đang xử lý" và chạy thao tác CSDL có bắt lỗi.</summary>
public abstract partial class ViewModelBase(Notifier notify) : ObservableObject
{
    protected Notifier Notify { get; } = notify;

    [ObservableProperty] private bool _dangXuLy;

    /// <summary>Chạy một thao tác async; lỗi hiện Snackbar. Trả về true nếu thành công.</summary>
    protected async Task<bool> ChayAsync(Func<Task> thaoTac, string? thongBaoThanhCong = null)
    {
        DangXuLy = true;
        try
        {
            await thaoTac();
            if (thongBaoThanhCong != null) Notify.ThanhCong(thongBaoThanhCong);
            return true;
        }
        catch (Exception ex)
        {
            Notify.Loi(ex);
            return false;
        }
        finally
        {
            DangXuLy = false;
        }
    }

    /// <summary>Gọi khi trang được mở.</summary>
    public virtual Task TaiAsync() => Task.CompletedTask;

    /// <summary>Gọi khi rời trang (hủy đăng ký sự kiện thiết bị...).</summary>
    public virtual void RoiTrang() { }
}
