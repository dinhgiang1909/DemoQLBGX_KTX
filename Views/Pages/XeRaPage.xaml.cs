using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QLBGX.Devices;
using QLBGX.ViewModels;

namespace QLBGX.Views.Pages;

/// <summary>Code-behind chỉ xử lý con trỏ / phím (không có logic nghiệp vụ).</summary>
public partial class XeRaPage : Page, IPhimTat
{
    private readonly XeRaViewModel _vm;

    public XeRaPage(XeRaViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        vm.YeuCauFocus += (_, o) => Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            switch (o)
            {
                case "BienSoTim": TxtBienSoTim.Focus(); break;
                case "BienSoRa": TxtBienSoRa.Focus(); break;
                default: CboThe.Focus(); break;
            }
        });
        Loaded += async (_, _) => await vm.TaiAsync();
        Unloaded += (_, _) => vm.RoiTrang();
    }

    private void CboThe_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        CboThe.IsDropDownOpen = false;
        _vm.TimLuotCommand.Execute(null);
        e.Handled = true;
    }

    private void CboThe_DropDownClosed(object sender, EventArgs e)
    {
        if (CboThe.SelectedItem is GoiYThe g)
        {
            _vm.MaTheNhap = g.MaThe;
            _vm.TimLuotCommand.Execute(null);
        }
    }

    /// <summary>Phím tắt: F2 chụp ảnh, F5 xác nhận, Esc làm mới.</summary>
    public bool XuLyPhim(Key phim)
    {
        System.Windows.Input.ICommand? cmd = phim switch
        {
            Key.F2 => _vm.ChupAnhCommand,
            Key.F5 => _vm.ThanhToanCommand,
            Key.Escape when !CboThe.IsDropDownOpen => _vm.LamMoiCommand,
            _ => null,
        };
        if (cmd == null) return false;
        if (cmd.CanExecute(null)) cmd.Execute(null);
        return true;
    }
}
