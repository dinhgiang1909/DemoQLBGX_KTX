using QLBGX.ViewModels;
using Wpf.Ui.Controls;

namespace QLBGX.Views;

public partial class DoiMatKhauWindow : FluentWindow
{
    public DoiMatKhauWindow(DoiMatKhauViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.DaDoiXong += (_, _) => DialogResult = true;
        InitializeComponent();
    }
}
