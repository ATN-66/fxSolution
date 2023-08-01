using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Controls;
using Terminal.WinUI3.Controls.Chart.Candlestick;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Entities;
using Terminal.WinUI3.Models.Kernel;
using ICoordinator = Terminal.WinUI3.Contracts.Services.ICoordinator;

namespace Terminal.WinUI3.ViewModels;

public sealed class SymbolOfCurrencyViewModel : SymbolViewModelBase
{
    public SymbolOfCurrencyViewModel(IConfiguration configuration, IChartService chartService, ICoordinator coordinator, IAccountService accountService, IDispatcherService dispatcherService) : base(configuration, chartService, coordinator, accountService, dispatcherService)
    {
    }

    public async void LoadChart()
    {
        ChartControlBase = await ChartService.GetChartAsync<CandlestickChartControl, Candlestick, CandlestickKernel>(Symbol, ChartType.Candlesticks, IsReversed).ConfigureAwait(false);
        UpdateProperties();
    }

    public override void OnNavigatedTo(object parameter) => throw new NotImplementedException("SymbolOfCurrencyViewModel: OnNavigatedTo");

    public override void OnNavigatedFrom()
    {
        ChartService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }
}