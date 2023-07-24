/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                    SymbolPage.cs |
  +------------------------------------------------------------------+*/

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
}