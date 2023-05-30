/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                        EURUSD.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class EURUSDPage
{
    public EURUSDPage()
    {
        ViewModel = App.GetService<EURUSDViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
        ContentArea.Children.Add(ViewModel.TickChartControl);
    }

    public EURUSDViewModel ViewModel
    {
        get;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ContentArea.Children.Remove(ViewModel.TickChartControl);
    }
}