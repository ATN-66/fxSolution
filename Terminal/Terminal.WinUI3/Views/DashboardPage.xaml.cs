/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                 DashboardPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

    private DashboardViewModel ViewModel
    {
        get;
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

        args.ItemContainer.IsEnabled = item.IsEnabled & !item.IsSelected;

        if (item.Id != ViewModel.SelectedItem || !item.IsEnabled)
        {
            return;
        }

        args.ItemContainer.Foreground = new SolidColorBrush(Colors.Yellow);
        ToolTipService.SetToolTip(args.ItemContainer, "This item is currently selected");
    }
    
    private void OnItemGridViewItemClick(object sender, ItemClickEventArgs e)
    {
        var item = (DashboardItem)e.ClickedItem;
        ViewModel.SelectedItem = item.Id;
    }
}