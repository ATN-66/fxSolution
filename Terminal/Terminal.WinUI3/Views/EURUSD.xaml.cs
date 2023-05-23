/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                                        EURUSD.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Microsoft.UI.Xaml.Navigation;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class EURUSD
{
    private readonly IVisualService _visualService;
    private BaseChartControl? _chartControl;

    public EURUSD()
    {
        ViewModel = App.GetService<EURUSDViewModel>();
        _visualService = App.GetService<IVisualService>();
        InitializeComponent();
        CreateChartControl();
    }

    public EURUSDViewModel ViewModel
    {
        get;
    }

    private void CreateChartControl()
    {
        var symbol = (Symbol)Enum.Parse(typeof(Symbol), GetType().Name);
        _chartControl = _visualService.GetChartControl(symbol);
        ContentArea.Children.Add(_chartControl);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_chartControl == null)
        {
            return;
        }

        _chartControl.Detach();
        ContentArea.Children.Remove(_chartControl);
        _chartControl = null;
    }
}