/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                 DashboardPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public partial class DashboardPage
{
    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
        itemsCVS.Source = ViewModel.GroupList;
    }

    public DashboardViewModel ViewModel
    {
        get;
    }

    private void OnItemGridViewLoaded(object sender, RoutedEventArgs e)
    {
       
    }

    private void OnItemGridViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        
    }
    
    private void OnItemGridViewItemClick(object sender, ItemClickEventArgs e)
    {
       
    }
}