/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
                                                     CurrencyPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public partial class CurrencyPage
{
    public CurrencyPage()
    {
        ViewModel = App.GetService<CurrencyViewModel>();
        InitializeComponent();
    }

    public CurrencyViewModel ViewModel
    {
        get;
    }
}