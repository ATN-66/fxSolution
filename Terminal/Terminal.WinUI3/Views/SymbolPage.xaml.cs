/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                    SymbolPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class SymbolPage
{
    public SymbolPage()
    {
        ViewModel = App.GetService<SymbolViewModel>();
        InitializeComponent();
    }

    public SymbolViewModel ViewModel
    {
        get;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ContentArea.Children.Add(ViewModel.ChartControlBase);
    } 

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ContentArea.Children.Remove(ViewModel.ChartControlBase);
    }
}