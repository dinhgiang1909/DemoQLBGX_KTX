using QLBGX.ViewModels;
using Wpf.Ui.Controls;

namespace QLBGX.Views;

public partial class LoginWindow : FluentWindow
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow(LoginViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
