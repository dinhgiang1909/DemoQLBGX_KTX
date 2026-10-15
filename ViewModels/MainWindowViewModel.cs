using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLBGX.Helpers;
using QLBGX.Services;

namespace QLBGX.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly WindowService _windows;
    private readonly DispatcherTimer _dongHo;

    [ObservableProperty] private string _gioHienTai = string.Empty;

    public Session Session { get; }

    public bool HienDanhMuc => Permissions.DanhMuc(Session);

    public MainWindowViewModel(Session session, WindowService windows)
    {
        Session = session;
        _windows = windows;
        CapNhatGio();
        _dongHo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _dongHo.Tick += (_, _) => CapNhatGio();
        _dongHo.Start();
    }

    private void CapNhatGio() => GioHienTai = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy");

    public void DungDongHo() => _dongHo.Stop();

    [RelayCommand]
    private void DoiMatKhau() => _windows.MoDoiMatKhau();

    [RelayCommand]
    private void DangXuat() => _windows.DangXuat();
}
