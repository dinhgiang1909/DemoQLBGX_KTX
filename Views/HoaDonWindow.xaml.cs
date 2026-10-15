using System.Windows;
using QLBGX.Models.Dtos;
using QLBGX.Services;
using Wpf.Ui.Controls;

namespace QLBGX.Views;

public partial class HoaDonWindow : FluentWindow
{
    public HoaDonWindow(HoaDonInDto hoaDon)
    {
        DataContext = hoaDon;
        InitializeComponent();
        Title = $"Hóa đơn #{hoaDon.MaHoaDon}";
    }

    private void In_Click(object sender, RoutedEventArgs e) => PrintService.InVisual(Phieu, Title);

    private void Dong_Click(object sender, RoutedEventArgs e) => Close();
}
