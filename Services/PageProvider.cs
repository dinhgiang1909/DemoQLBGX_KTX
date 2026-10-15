using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions;

namespace QLBGX.Services;

/// <summary>Cho NavigationView tạo trang qua DI (trang được tiêm ViewModel/Service vào constructor).</summary>
public class PageProvider(IServiceProvider sp) : INavigationViewPageProvider
{
    public object? GetPage(Type pageType) => ActivatorUtilities.GetServiceOrCreateInstance(sp, pageType);
}
