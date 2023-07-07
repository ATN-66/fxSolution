/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                        TicksContributionsPage.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TicksContributionsPage
{
    private bool _isDrag;
    private double _originalHeight;
    private double _originalWidth;
    private double _originalMousePosition;

    public TicksContributionsPage()
    {
        ViewModel = App.GetService<TicksContributionsViewModel>();
        InitializeComponent();

        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("TicksContributionsPage_FirstRowDefinition_Height", out var height))
        {
            FirstRowDefinition.Height = new GridLength(Convert.ToInt32(height));
        }
        else
        {
            ApplicationData.Current.LocalSettings.Values.Add("TicksContributionsPage_FirstRowDefinition_Height", 100.ToString());
            FirstRowDefinition.Height = new GridLength(Convert.ToInt32(100));
        }

        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("TicksContributionsPage_FirstColumnDefinition_Width", out var width))
        {
            FirstColumnDefinition.Width = new GridLength(Convert.ToInt32(width));
        }
        else
        {
            ApplicationData.Current.LocalSettings.Values.Add("TicksContributionsPage_FirstColumnDefinition_Width", 100.ToString());
            FirstColumnDefinition.Width = new GridLength(Convert.ToInt32(100));
        }
    }

    public List<int> DotsCollection
    {
        get;
    } = new(Enumerable.Range(0, 2));

    public TicksContributionsViewModel ViewModel
    {
        get;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        ApplicationData.Current.LocalSettings.Values["TicksContributionsPage_FirstRowDefinition_Height"] = FirstRowDefinition.Height.Value.ToString(CultureInfo.InvariantCulture);
        ApplicationData.Current.LocalSettings.Values["TTicksContributionsPage_FirstColumnDefinition_Width"] = FirstColumnDefinition.Width.Value.ToString(CultureInfo.InvariantCulture);
    }

    private void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDrag = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
    }

    private void GridSplitterHorizontal_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDrag = true;
        _originalHeight = FirstRowDefinition.Height.Value;
        _originalMousePosition = e.GetCurrentPoint(this).Position.Y;
        ((UIElement)sender).CapturePointer(e.Pointer);
    }

    private void GridSplitterHorizontal_PointerMoved(object sender, PointerRoutedEventArgs e)
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

        FirstRowDefinition.Height = new GridLength(newHeight);
    }

    private void GridSplitterVertical_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDrag = true;
        _originalWidth = FirstColumnDefinition.Width.Value;
        _originalMousePosition = e.GetCurrentPoint(this).Position.X;
        ((UIElement)sender).CapturePointer(e.Pointer);
    }

    private void GridSplitterVertical_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrag)
        {
            return;
        }

        var currentPosition = e.GetCurrentPoint(this).Position.X;
        var difference = currentPosition - _originalMousePosition;

        var containerWidth = ActualWidth;
        const int rightColumnMinimumWidth = 30;
        var maxAvailableWidth = containerWidth - rightColumnMinimumWidth;

        var newWidth = Math.Max(0, _originalWidth + difference);
        newWidth = Math.Min(newWidth, maxAvailableWidth);

        FirstColumnDefinition.Width = new GridLength(newWidth);
    }
}