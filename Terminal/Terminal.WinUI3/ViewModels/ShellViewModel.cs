/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                                ShellViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Services.Messenger.Messages;
using Terminal.WinUI3.Views;

namespace Terminal.WinUI3.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty] private bool _isBackEnabled;
    [ObservableProperty] private object? _selected;
    public ObservableCollection<NavigationViewItemBase> NavigationItems { get; } = new();
    private readonly FontFamily _symbolThemeFontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"];

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        NavigationService.Navigated += OnNavigated;

        var dashboardItem = new NavigationViewItem
        {
            Content = "Dashboard",
            Tag = "Dashboard",
            Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = "\uF246" }
        };
        NavigationHelper.SetNavigateTo(dashboardItem, typeof(DashboardViewModel).FullName!);
        NavigationItems.Add(dashboardItem);

        var separator = new NavigationViewItemSeparator();
        NavigationItems.Add(separator);
    }

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<DashboardChangedMessage>(this, (r, m) =>
        {
            var tmp = m.Value.Id;
            Debug.WriteLine(tmp);
        });
    }
}
