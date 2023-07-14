/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                     ShellPage.cs |
  +------------------------------------------------------------------+*/

using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class ShellPage
{
    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        foreach (var item in ViewModel.NavigationItems)
        {
            NavigationViewControl.MenuItems.Add(item);
        }

        ViewModel.NavigationItems.CollectionChanged += NavigationItems_CollectionChanged;
    }

    public ShellViewModel ViewModel
    {
        get;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
    }

    private void NavigationViewControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
       //todo: nothing todo
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();
        var result = navigationService.GoBack();
        args.Handled = result;
    }

    private void NavigationViewControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsActive = true;
    }

    private void NavigationViewControl_OnUnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsActive = false;
    }

    private void NavigationItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)//todo
    {
        if (e.NewItems != null)
        {
            foreach (NavigationViewItemBase newItem in e.NewItems)
            {
                NavigationViewControl.MenuItems.Add(newItem);
            }
        }

        if (e.OldItems == null)
        {
            return;
        }

        foreach (NavigationViewItemBase oldItem in e.OldItems)
        {
            NavigationViewControl.MenuItems.Remove(oldItem);
        }
    }
}   