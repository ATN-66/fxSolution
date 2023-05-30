using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3;

public abstract class PageBase : Page, INotifyPropertyChanged
{
    private string _itemId;

    private IEnumerable<DashboardItem> _items;

    public IEnumerable<DashboardItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnItemGridViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (sender.ContainerFromItem(sender.Items.LastOrDefault()) is GridViewItem container)
        {
            container.XYFocusDown = container;
        }

        //var item = args.Item as ControlInfoDataItem;
        //if (item != null)
        //{
        //    args.ItemContainer.IsEnabled = item.IncludedInBuild;
        //}
    }

    protected void OnItemGridViewItemClick(object sender, ItemClickEventArgs e)
    {
        var gridView = (GridView)sender;
        var item = (DashboardItem)e.ClickedItem;

        _itemId = item.UniqueId;

        throw new NotImplementedException();
        //NavigationRootPage.GetForElement(this).Navigate(typeof(ItemPage), _itemId, new DrillInNavigationTransitionInfo());
    }

    protected void OnItemGridViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_itemId != null)
        {
            var gridView = (GridView)sender;
            var items = gridView.ItemsSource as IEnumerable<DashboardItem>;
            var item = items?.FirstOrDefault(s => s.UniqueId == _itemId);
            if (item != null)
            {
                gridView.ScrollIntoView(item);
                ((GridViewItem)gridView.ContainerFromItem(item))?.Focus(FocusState.Programmatic);
            }
        }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}