﻿/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
                                                          EURPage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class EURPage
{
    public EURPage()
    {
        ViewModel = App.GetService<EURViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
    }

    public EURViewModel ViewModel
    {
        get;
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