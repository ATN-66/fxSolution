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
        Configure<PlaceHolderViewModel, PlaceHolderPage>();
        Configure<DashboardViewModel, DashboardPage>();
        Configure<CurrenciesOverviewViewModel, CurrenciesOverviewPage>();
        Configure<CurrencyViewModel, CurrencyPage>();
        Configure<SymbolViewModel, SymbolPage>();
        Configure<SymbolPlusViewModel, SymbolPlusPage>();
        Configure<HistoricalDataOverviewViewModel, HistoricalDataOverviewPage>();
        Configure<AccountOverviewViewModel, AccountOverviewPage>();
        Configure<TradingHistoryViewModel, TradingHistoryPage>();
        Configure<AccountPropertiesViewModel, AccountPropertiesPage>();
        Configure<TicksContributionsViewModel, TicksContributionsPage>();
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
                    $"This tradeType is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}