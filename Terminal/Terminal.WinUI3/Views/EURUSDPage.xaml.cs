/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                        EURUSD.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class EURUSDPage
{
    public EURUSDPage()
    {
        ViewModel = App.GetService<EURUSDViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
        ContentArea.Children.Add(ViewModel.Chart);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public EURUSDViewModel ViewModel
    {
        get;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ContentArea.Children.Remove(ViewModel.Chart);
    }

    private void StackPanel_MouseEnter(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
    {
        if (sender is FrameworkElement fe && fe.FindName("PageCommandBar") is CommandBar cb)
        {
            cb.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
        }
    }

    private void StackPanel_MouseExited(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
    {
        if (sender is FrameworkElement fe && fe.FindName("PageCommandBar") is CommandBar cb)
        {
            cb.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
        }
    }

    private void CommandBar_Opening(object? sender, object e)
    {
        var cb = sender as CommandBar;
        if (cb != null)
        {
            cb.Background.Opacity = 1.0;
        }
    }

    private void CommandBar_Closing(object? sender, object e)
    {
        var cb = sender as CommandBar;
        if (cb != null)
        {
            cb.Background.Opacity = 0.5;
        }
    }

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {

    }
}