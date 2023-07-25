using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;

namespace Terminal.WinUI3.ViewModels;

public sealed class SymbolOfCurrencyViewModel : SymbolViewModelBase
{
    public SymbolOfCurrencyViewModel(IVisualService visualService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService) : base(visualService, processor, accountService, dispatcherService)
    {
    }

    public void LoadChart()
    {
        ChartControlBase = VisualService.GetChart<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed);
        UpdateProperties();
    }

    public override void OnNavigatedTo(object parameter) => throw new NotImplementedException("SymbolOfCurrencyViewModel: OnNavigatedTo");

    public override void OnNavigatedFrom() => throw new NotImplementedException("SymbolOfCurrencyViewModel: OnNavigatedFrom");
}