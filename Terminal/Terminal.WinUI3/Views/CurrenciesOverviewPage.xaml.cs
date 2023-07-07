/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                        CurrenciesOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class CurrenciesOverviewPage
{
    public CurrenciesOverviewPage()
    {
        ViewModel = App.GetService<CurrenciesOverviewViewModel>();
        InitializeComponent();
    }

    public CurrenciesOverviewViewModel ViewModel
    {
        get;
    }
}