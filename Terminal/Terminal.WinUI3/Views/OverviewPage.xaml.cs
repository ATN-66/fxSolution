/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                  OverviewPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class OverviewPage
{
    public OverviewPage()
    {
        ViewModel = App.GetService<OverviewViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
    }

    public OverviewViewModel ViewModel
    {
        get;
    }
}