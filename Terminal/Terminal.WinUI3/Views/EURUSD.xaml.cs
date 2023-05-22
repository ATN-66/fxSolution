using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class EURUSD : Page
{
    public EURUSD()
    {
        ViewModel = App.GetService<EURUSDViewModel>();
        InitializeComponent();
    }

    public EURUSDViewModel ViewModel
    {
        get;
    }
}