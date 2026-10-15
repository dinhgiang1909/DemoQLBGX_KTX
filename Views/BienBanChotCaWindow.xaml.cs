using System.Windows;
using QLBGX.Models;
using QLBGX.Services;
using Wpf.Ui.Controls;

namespace QLBGX.Views;

public partial class BienBanChotCaWindow : FluentWindow
{
    public BienBanChotCaWindow(LichSuCaTruc phien)
    {
        DataContext = phien;
        InitializeComponent();
        Title = $"Biên bản chốt ca #{phien.MaLichSuCa}";
    }

    private void In_Click(object sender, RoutedEventArgs e) => PrintService.InVisual(Phieu, Title);

    private void Dong_Click(object sender, RoutedEventArgs e) => Close();
}
