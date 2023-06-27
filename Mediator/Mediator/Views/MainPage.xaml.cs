using Mediator.ViewModels;
using Microsoft.UI.Xaml;

namespace Mediator.Views;

public sealed partial class MainPage
{
    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();

        App.MainWindow.ExtendsContentIntoTitleBar = false;
        App.MainWindow.Activated += MainWindow_Activated;
    }

    public MainViewModel ViewModel
    {
        get;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsActive = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsActive = false;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
    }
}