/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                                ShellViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Reflection;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ShellViewModel> _logger;

    [ObservableProperty] private bool _isBackEnabled;
    [ObservableProperty] private object? _selected;
    public ObservableCollection<NavigationViewItemBase> NavigationItems { get; } = new();
    private readonly FontFamily _symbolThemeFontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"];

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService, ILogger<ShellViewModel> logger)
    {
        _logger = logger;

        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        NavigationService.Navigated += OnNavigated;
        InitializeNavigationView();
    }

    private void InitializeNavigationView()
    {
        while (NavigationItems.Count > 0)
        {
            NavigationItems.RemoveAt(0);
        }

        var dashboardItem = new NavigationViewItem
        {
            Content = "Dashboard",
            Tag = "dashboard_tag",
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
        Messenger.Register<DashboardChangedMessage>(this, (_, m) =>
        {
            try
            {
                InitializeNavigationView();
                var item = m.Value.DashboardItem;
                var mainItem = item.MainItem;
                var mainPage = Type.GetType($"{Assembly.GetExecutingAssembly().GetName().Name}.ViewModels.{mainItem.NavigateTo}")!.FullName;
                var mainViewItem = new NavigationViewItem
                {
                    Content = mainItem.Content,
                    Tag = mainItem.Tag,
                    Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = mainItem.Glyph }
                };

                NavigationHelper.SetNavigateTo(mainViewItem, mainPage!);
                NavigationItems.Add(mainViewItem);

                if (item.NavigationItems != null)
                {
                    foreach (var navigationItem in item.NavigationItems)
                    {
                        var navigationViewItem = new NavigationViewItem
                        {
                            Content = navigationItem.Content,
                            Tag = navigationItem.Tag,
                            Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = navigationItem.Glyph }
                        };

                        var navigationPage = Type.GetType($"{Assembly.GetExecutingAssembly().GetName().Name}.ViewModels.{navigationItem.NavigateTo}")!.FullName;
                        NavigationHelper.SetNavigateTo(navigationViewItem, navigationPage!);
                        NavigationItems.Add(navigationViewItem);

                        if (navigationItem.MenuItems == null)
                        {
                            continue;
                        }

                        foreach (var menuItem in navigationItem.MenuItems)
                        {
                            var menuViewItem = new NavigationViewItem
                            {
                                Content = menuItem.Content,
                                Tag = menuItem.Tag,
                                Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = menuItem.Glyph }
                            };

                            var menuPage = Type.GetType($"{Assembly.GetExecutingAssembly().GetName().Name}.ViewModels.{menuItem.NavigateTo}")!.FullName;
                            NavigationHelper.SetNavigateTo(menuViewItem, menuPage!);
                            navigationViewItem.MenuItems.Add(menuViewItem);
                        }
                    }
                }

                NavigationService.NavigateTo(mainPage!, mainViewItem.Tag);
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, "");
                throw;
            }
        });
    }
}
