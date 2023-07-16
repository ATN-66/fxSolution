/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                            TradingHistoryPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TradingHistoryPage
{
    public TradingHistoryPage()
    {
        ViewModel = App.GetService<TradingHistoryViewModel>();
        InitializeComponent();
    }

    public TradingHistoryViewModel ViewModel
    {
        get;
    }
}