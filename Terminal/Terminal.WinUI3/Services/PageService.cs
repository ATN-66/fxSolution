using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;
using Terminal.WinUI3.Views;

namespace Terminal.WinUI3.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
        Configure<DashboardViewModel, DashboardPage>();

        Configure<USDViewModel, USDPage>();
        Configure<EURViewModel, EURPage>();
        Configure<GBPViewModel, GBPPage>();
        Configure<JPYViewModel, JPYPage>();

        Configure<EURUSDViewModel, EURUSDPage>();
        Configure<USDEURViewModel, USDEURPage>();
        Configure<GBPUSDViewModel, GBPUSDPage>();

        Configure<USDGBPViewModel, USDGBPPage>();

        Configure<EURGBPViewModel, EURGBPPage>();

        Configure<GBPEURViewModel, GBPEURPage>();

        Configure<USDJPYViewModel, USDJPYPage>();
        Configure<JPYUSDViewModel, JPYUSDPage>();
        Configure<EURJPYViewModel, EURJPYPage>();

        Configure<JPYEURViewModel, JPYEURPage>();


        Configure<GBPJPYViewModel, GBPJPYPage>();
        Configure<JPYGBPViewModel, JPYGBPPage>();

        Configure<SettingsViewModel, SettingsPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<TVm, TV>() where TVm : ObservableObject where TV : Page
    {
        lock (_pages)
        {
            var key = typeof(TVm).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(TV);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException(
                    $"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}