using System.Media;

namespace QLBGX.Devices;

/// <summary>Barrier cổng. Bản mô phỏng phát âm "beep" và báo trạng thái "mở" trong 3 giây.</summary>
public interface IBarrier
{
    bool DangMo { get; }
    event EventHandler<bool>? TrangThaiThayDoi;
    Task MoAsync();
}

public class SimulatedBarrier : IBarrier
{
    private int _phien;

    public bool DangMo { get; private set; }
    public event EventHandler<bool>? TrangThaiThayDoi;

    public async Task MoAsync()
    {
        var phien = Interlocked.Increment(ref _phien);
        DatTrangThai(true);
        SystemSounds.Beep.Play();
        await Task.Delay(3000);
        if (phien == _phien) DatTrangThai(false);   // nếu vừa mở lần nữa thì lần sau tự đóng
    }

    private void DatTrangThai(bool mo)
    {
        DangMo = mo;
        TrangThaiThayDoi?.Invoke(this, mo);
    }
}
