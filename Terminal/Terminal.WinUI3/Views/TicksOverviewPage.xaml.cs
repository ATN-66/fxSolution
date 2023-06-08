/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                             TicksOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TicksOverviewPage
{
    public TicksOverviewPage()
    {
        ViewModel = App.GetService<TicksOverviewViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
    }

    public TicksOverviewViewModel ViewModel
    {
        get;
    }
}