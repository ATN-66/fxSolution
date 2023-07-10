/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                        TicksContributionsPage.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class TicksContributionsPage
{
    private bool _isDrag;
    private double _originalHeight;
    private double _originalWidth;
    private double _originalMousePosition;

    private readonly ILocalSettingsService _localSettingsService;
    private readonly IDispatcherService _dispatcherService;

    private static string TicksContributionsPageFirstColumnDefinitionWidth => "TicksContributionsPage_FirstColumnDefinition_Width";
    private static string TicksContributionsPageFirstRowDefinitionHeight => "TicksContributionsPage_FirstRowDefinition_Height";

    public TicksContributionsPage()
    {
        InitializeComponent();

        ViewModel = App.GetService<TicksContributionsViewModel>();
        _localSettingsService = App.GetService<ILocalSettingsService>();
        _dispatcherService = App.GetService<IDispatcherService>();
    }

    public List<int> DotsCollection
    {
        get;
    } = new(Enumerable.Range(0, 2));

    public TicksContributionsViewModel ViewModel
    {
        get;
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            var widthSetting = await _localSettingsService.ReadSettingAsync<string>(TicksContributionsPageFirstColumnDefinitionWidth).ConfigureAwait(true);
            var heightSetting = await _localSettingsService.ReadSettingAsync<string>(TicksContributionsPageFirstRowDefinitionHeight).ConfigureAwait(true);

            if (int.TryParse(widthSetting, out var width) && int.TryParse(heightSetting, out var height))
            {
                await _dispatcherService.ExecuteOnUIThreadAsync(() =>
                {
                    FirstColumnDefinition.Width = new GridLength(Convert.ToInt32(width));
                    FirstRowDefinition.Height = new GridLength(Convert.ToInt32(height));
                }).ConfigureAwait(true);
            }
            else
            {
                FirstColumnDefinition.Width = new GridLength(Convert.ToInt32(100));
                FirstRowDefinition.Height = new GridLength(Convert.ToInt32(100));
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            throw;
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);

        try
        {
            async void Action()
            {
                await _localSettingsService.SaveSettingAsync(TicksContributionsPageFirstColumnDefinitionWidth, FirstColumnDefinition.Width.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(true);
                await _localSettingsService.SaveSettingAsync(TicksContributionsPageFirstRowDefinitionHeight, FirstRowDefinition.Height.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(true);
            }

            _dispatcherService.ExecuteOnUIThreadAsync(Action);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            throw;
        }
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