using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLBGX.Devices;
using QLBGX.Helpers;
using QLBGX.Models;
using QLBGX.Services;
using QLBGX.ViewModels;
using QLBGX.Views;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace QLBGX;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Giao diện tiếng Việt: ngày dd/MM/yyyy, số 1.234
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(vi.IetfLanguageTag)));

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var connStr = config.GetConnectionString("BaiGiuXe");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            MessageBox.Show("Thiếu chuỗi kết nối ConnectionStrings:BaiGiuXe trong appsettings.json.",
                "Lỗi cấu hình", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        Services = ConfigureServices(config, connStr);
        Services.GetRequiredService<WindowService>().MoDangNhap();
    }

    private static ServiceProvider ConfigureServices(IConfiguration config, string connStr)
    {
        var services = new ServiceCollection();

        services.AddSingleton(config);
        services.AddDbContextFactory<QuanLyBaiGiuXeKTXContext>(o => o.UseSqlServer(connStr));

        // Hạ tầng
        services.AddSingleton<Session>();
        services.AddSingleton<WindowService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<INavigationViewPageProvider, PageProvider>();

        services.AddSingleton<Notifier>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<AppNavigator>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<PrintService>();

        // Thiết bị (mô phỏng)
        services.AddSingleton<ImageStore>();
        services.AddSingleton<ICardReader, SimulatedCardReader>();
        services.AddSingleton<IBarrier, SimulatedBarrier>();
        if (bool.TryParse(config["Anpr:Enabled"], out var anpr) && anpr)
            services.AddSingleton<IBienSoReader>(_ => new ExeBienSoReader(
                config["Anpr:ExePath"] ?? "Tools/ANPR/anpr.exe",
                int.TryParse(config["Anpr:TimeoutMs"], out var ms) ? ms : 8000));
        else
            services.AddSingleton<IBienSoReader, ManualBienSoReader>();

        // Service nghiệp vụ
        services.AddSingleton<AuthService>();
        services.AddSingleton<XeVaoService>();
        services.AddSingleton<XeRaService>();
        services.AddSingleton<HoaDonService>();
        services.AddSingleton<VeThangService>();
        services.AddSingleton<CaTrucService>();
        services.AddSingleton<TheService>();
        services.AddSingleton<DanhMucService>();
        services.AddSingleton<BaoCaoService>();

        // ViewModel + cửa sổ
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DoiMatKhauViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<DoiMatKhauWindow>();
        services.AddTransient<MainWindow>();

        // ViewModel của các trang (trang được tạo qua PageProvider)
        foreach (var t in typeof(App).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false, Namespace: "QLBGX.ViewModels" }
                                 && t.IsSubclassOf(typeof(ViewModelBase))))
            services.AddTransient(t);

        return services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(SqlErrorHelper.ThongDiep(e.Exception), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
