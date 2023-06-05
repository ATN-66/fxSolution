/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                             TicksOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TicksOverviewPage
{
    public TicksOverviewPage()
    {
        ViewModel = App.GetService<TicksOverviewViewModel>();
        InitializeComponent();
        gridView.ItemsSource = ViewModel.Items;
    }

    public TicksOverviewViewModel ViewModel
    {
        get;
    }
}