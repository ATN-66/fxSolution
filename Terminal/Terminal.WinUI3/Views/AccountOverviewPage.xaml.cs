/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                           AccountOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class AccountOverviewPage
{
    public AccountOverviewPage()
    {
        ViewModel = App.GetService<AccountOverviewViewModel>();
        InitializeComponent();
    }

    public AccountOverviewViewModel ViewModel
    {
        get;
    }
}