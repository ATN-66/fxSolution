using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal.WinUI3.Helpers;

// Helper class to set the navigation target for a NavigationViewItem.
//
// Usage in XAML:
// <NavigationViewItem x:Uid="Shell_Main" Icon="Document" helpers:NavigationHelper.NavigateTo="AppName.ViewModels.DashboardViewModel" />
//
// Usage in code:
// NavigationHelper.SetNavigateTo(navigationViewItem, typeof(DashboardViewModel).FullName);
public class NavigationHelper
{
    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached("NavigateTo", typeof(string), typeof(NavigationHelper), new PropertyMetadata(null));

    public static string GetNavigateTo(NavigationViewItem item) => (string)item.GetValue(NavigateToProperty);

    public static void SetNavigateTo(NavigationViewItem item, string value) => item.SetValue(NavigateToProperty, value);
}