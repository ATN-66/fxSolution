/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                 DashboardPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Models.Dashboard;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public partial class DashboardPage
{
    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
        itemsCVS.Source = ViewModel.Groups;
    }

    public DashboardViewModel ViewModel
    {
        get;
    }

    private void OnItemGridViewLoaded(object sender, RoutedEventArgs e)//todo:
    {
        var gridView = (GridView)sender;
        var groupTitleLists = ViewModel.Groups;
        var items = groupTitleLists.SelectMany(g => g).OfType<DashboardItem>();
        var item = items.FirstOrDefault(s => s.Id == ViewModel.SelectedItem);
        if (item == null)
        {
            return;
        }

        gridView.ScrollIntoView(item);
        ((GridViewItem)gridView.ContainerFromItem(item))?.Focus(FocusState.Programmatic);
    }

    private void OnItemGridViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (sender.ContainerFromItem(sender.Items.LastOrDefault()) is GridViewItem container)
        {
            container.XYFocusDown = container;
        }

        if (args.Item is not DashboardItem item)
        {
            return;
        }

        args.ItemContainer.IsEnabled = item.IsEnabled;
        if (item.IsSelected)
        {
            args.ItemContainer.IsEnabled = false;
        }
    }
    
    private void OnItemGridViewItemClick(object sender, ItemClickEventArgs e)
    {
        var item = (DashboardItem)e.ClickedItem;
        ViewModel.SelectedItem = item.Id;
    }
}