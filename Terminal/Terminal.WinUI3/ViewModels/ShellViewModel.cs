/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                                ShellViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Services;
using Terminal.WinUI3.Services.Messenger.Messages;
using Terminal.WinUI3.Views;

namespace Terminal.WinUI3.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private IConfiguration _configuration;
    [ObservableProperty] private bool _isBackEnabled;
    [ObservableProperty] private object? _selected;
    public ObservableCollection<NavigationViewItemBase> NavigationItems { get; } = new();
    private readonly FontFamily _symbolThemeFontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"];

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService, IConfiguration configuration)
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        _configuration = configuration;

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

        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("MainWindowWidth", out var width))
        {
            App.MainWindow.Width = width != null ? Convert.ToInt32(width) : Convert.ToInt32(_configuration.GetValue<int>("MainWindowWidth"));
        }
        else
        {
            ApplicationData.Current.LocalSettings.Values.Add("MainWindowWidth", _configuration.GetValue<int>("MainWindowWidth"));
        }

        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("MainWindowHeight", out var height))
        {
            App.MainWindow.Height = height != null ? Convert.ToInt32(height) : Convert.ToInt32(_configuration.GetValue<int>("MainWindowHeight"));
        }
        else
        {
            ApplicationData.Current.LocalSettings.Values.Add("MainWindowHeight", _configuration.GetValue<int>("MainWindowHeight"));
        }

        Messenger.Register<DashboardChangedMessage>(this, (_, m) =>
        {
            var item = m.Value.DashboardItem;
            var pageToNavigate = string.Empty;

            foreach (var navigationItem in item.NavigationItems)
            {
                var viewItem = new NavigationViewItem
                {
                    Content = navigationItem.Content,
                    Tag = navigationItem.Tag,
                    Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = navigationItem.Glyph }
                };

                var page = Type.GetType($"{Assembly.GetExecutingAssembly().GetName().Name}.ViewModels.{navigationItem.NavigateTo}")!.FullName;
                if (navigationItem.IsMain)
                {
                    pageToNavigate = page;
                }
                NavigationHelper.SetNavigateTo(viewItem, page!);
                NavigationItems.Add(viewItem);
            }

            NavigationService.NavigateTo(pageToNavigate!);
        });
    }
}