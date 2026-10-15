using System.Windows.Controls;
using QLBGX.ViewModels;

namespace QLBGX.Views.Pages;

public partial class KhoThePage : Page
{
    public KhoThePage(KhoTheViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.TaiAsync();
        Unloaded += (_, _) => vm.RoiTrang();
    }
}
