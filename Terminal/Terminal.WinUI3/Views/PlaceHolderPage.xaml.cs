using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class PlaceHolderPage : Page
{
    public PlaceHolderPage()
    {
        ViewModel = App.GetService<PlaceHolderViewModel>();
        InitializeComponent();
    }

    public PlaceHolderViewModel ViewModel
    {
        get;
    }
}