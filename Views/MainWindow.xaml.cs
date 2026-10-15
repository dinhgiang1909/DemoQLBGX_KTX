using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QLBGX.ViewModels;
using QLBGX.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace QLBGX.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationViewPageProvider pageProvider,
        ISnackbarService snackbar, IContentDialogService dialogs, Services.AppNavigator navigator)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        // Ẩn mục menu không có quyền (Bảng 3.1)
        MenuDanhMuc.Visibility = viewModel.HienDanhMuc ? Visibility.Visible : Visibility.Collapsed;

        RootNavigation.SetPageProviderService(pageProvider);
        navigator.Gan(RootNavigation);
        snackbar.SetSnackbarPresenter(RootSnackbar);
        dialogs.SetDialogHost(RootContentDialog);

        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
        Closed += (_, _) => viewModel.DungDongHo();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    /// <summary>Chuyển phím tắt cho trang đang chứa con trỏ (bắt ở cửa sổ để control con không nuốt phím F5).</summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var phim = e.Key == Key.System ? e.SystemKey : e.Key;
        if (phim is not (Key.F2 or Key.F5 or Key.Escape)) return;
        for (var d = Keyboard.FocusedElement as DependencyObject; d != null; d = LayCha(d))
        {
            if (d is IPhimTat trang)
            {
                if (trang.XuLyPhim(phim)) e.Handled = true;
                return;
            }
        }
    }

    private static DependencyObject? LayCha(DependencyObject d) =>
        d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d)
            : LogicalTreeHelper.GetParent(d);
}
