/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                FileSourcePage.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Behaviors;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class FileSourcePage
{
    public FileSourcePage()
    {
        ViewModel = App.GetService<FileSourceViewModel>();
        InitializeComponent();
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding { Source = ViewModel, Mode = BindingMode.OneWay });
    }

    public FileSourceViewModel ViewModel
    {
        get;
    }
}