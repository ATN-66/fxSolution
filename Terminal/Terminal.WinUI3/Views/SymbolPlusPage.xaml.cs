/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                SymbolPlusPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class SymbolPlusPage
{
    public SymbolPlusPage()
    {
        ViewModel = App.GetService<SymbolPlusViewModel>();
        InitializeComponent();
    }

    public SymbolPlusViewModel ViewModel
    {
        get;
    }
}