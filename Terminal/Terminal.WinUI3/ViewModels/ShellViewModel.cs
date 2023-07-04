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
    public ObservableCollection<NavigationViewItemBase> NavigationItems { get; private init; } = new();
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
        Messenger.Register<DashboardChangedMessage>(this, (_, m) =>
        {
            try
            {
                InitializeNavigationView();

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
                    if (navigationItem.IsPageToNavigate)
                    {
                        pageToNavigate = page;
                    }
                    NavigationHelper.SetNavigateTo(viewItem, page!);
                    NavigationItems.Add(viewItem);

                    if (navigationItem.NavigationItems is not { Count: > 0 })
                    {
                        continue;
                    }

                    foreach (var navigationSubItem in navigationItem.NavigationItems)
                    {
                        var viewSubItem = new NavigationViewItem
                        {
                            Content = navigationSubItem.Content,
                            Tag = navigationSubItem.Tag,
                            Icon = new FontIcon { FontFamily = _symbolThemeFontFamily, Glyph = navigationSubItem.Glyph }
                        };

                        var subPage = Type.GetType($"{Assembly.GetExecutingAssembly().GetName().Name}.ViewModels.{navigationSubItem.NavigateTo}")!.FullName;
                        NavigationHelper.SetNavigateTo(viewSubItem, subPage!);
                        viewItem.MenuItems.Add(viewSubItem);
                    }
                }

                NavigationService.NavigateTo(pageToNavigate!);
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, "");
                throw;
            }
        });
    }
}