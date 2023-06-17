/*+------------------------------------------------------------------+
  |                                                  Common.Entities |
  |                                      BulkObservableCollection.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Common.Entities;

public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _isBulkOperation;

    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        _isBulkOperation = true;

        Clear();
        foreach (var item in items)
        {
            Add(item);
        }

        _isBulkOperation = false;

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]")); // No better alternative to this magic string
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_isBulkOperation)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_isBulkOperation)
        {
            base.OnPropertyChanged(e);
        }
    }
}