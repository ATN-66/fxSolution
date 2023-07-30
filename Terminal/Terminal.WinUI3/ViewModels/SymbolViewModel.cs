/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                               SymbolViewModel.cs |
  +------------------------------------------------------------------+*/

using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.AI.Interfaces;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.ViewModels;

public sealed class SymbolViewModel : SymbolViewModelBase
{
    public SymbolViewModel(IConfiguration configuration, IChartService chartService, IProcessor processor, IAccountService accountService, IDispatcherService dispatcherService) : base(configuration, chartService, processor, accountService, dispatcherService)
    {
       
    }

    public async override void OnNavigatedTo(object parameter)
    {
        var parts = parameter.ToString()?.Split(',');
        var symbolStr = parts?[0].Trim();
        if (Enum.TryParse<Symbol>(symbolStr, out var symbol))
        {
            Symbol = symbol;
        }
        else
        {
            throw new Exception($"Failed to parse '{symbolStr}' into a Symbol enum value.");
        }

        IsReversed = bool.Parse(parts?[1].Trim()!);

        ChartControlBase = await ChartService.GetDefaultChartAsync(Symbol, IsReversed).ConfigureAwait(true);
        UpdateProperties();
    }

    public override void OnNavigatedFrom()
    {
        ChartService.DisposeChart(ChartControlBase);
        ChartControlBase.Detach();
        ChartControlBase = null!;
    }
}