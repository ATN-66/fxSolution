/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                             TicksOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TicksOverviewPage
{
    private bool _isDrag;
    private double _originalHeight;
    private double _originalMousePosition;

    public TicksOverviewPage()
    {
        ViewModel = App.GetService<TicksOverviewViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
    }

    public List<int> DotsCollection
    {
        get;
    } = new(Enumerable.Range(0, 2));

    public TicksOverviewViewModel ViewModel
    {
        get;
    }

    private void GridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDrag = true;
        _originalHeight = Row0Definition.Height.Value;
        _originalMousePosition = e.GetCurrentPoint(this).Position.Y;
        ((UIElement)sender).CapturePointer(e.Pointer);
    }

    private void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDrag = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
    }

    private void GridSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrag)
        {
            return;
        }

        var currentPosition = e.GetCurrentPoint(this).Position.Y;
        var difference = currentPosition - _originalMousePosition;

        var containerHeight = ActualHeight;
        const int bottomRowMinimumHeight = 30;
        var maxAvailableHeight = containerHeight - bottomRowMinimumHeight;

        var newHeight = Math.Max(0, _originalHeight + difference);
        newHeight = Math.Min(newHeight, maxAvailableHeight);

        Row0Definition.Height = new GridLength(newHeight);
    }
}