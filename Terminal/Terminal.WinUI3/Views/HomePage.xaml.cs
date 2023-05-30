/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                      HomePage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class HomePage
{
    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }

    public HomeViewModel ViewModel
    {
        get;
    }
}