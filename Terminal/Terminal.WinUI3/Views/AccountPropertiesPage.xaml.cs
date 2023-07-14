/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |      \                                  AccountPropertiesPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class AccountPropertiesPage
{
    public AccountPropertiesPage()
    {
        ViewModel = App.GetService<AccountPropertiesViewModel>();
        InitializeComponent();
    }

    public AccountPropertiesViewModel ViewModel
    {
        get;
    }
}