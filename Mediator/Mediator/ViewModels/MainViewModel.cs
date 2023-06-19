/*+------------------------------------------------------------------+
  |                                               Mediator.ViewModels|
  |                                                 MainViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using Mediator.Contracts.Services;
using System.Collections.ObjectModel;
using Common.Entities;
using Mediator.Models;
using Microsoft.Extensions.Logging;
using Symbol = Common.Entities.Symbol;
using Microsoft.Extensions.Configuration;

namespace Mediator.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly CancellationTokenSource _cts;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<MainViewModel> _logger;
    
    private DeInitReason _reason;
    private int _ticksSaved;
    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;

    public event Action InitializationComplete = null!;

    public MainViewModel(IConfiguration configuration, IDispatcherService dispatcherService, CancellationTokenSource cts, ILogger<MainViewModel> logger)
    {
        _cts = cts;
        _dispatcherService = dispatcherService;
        _logger = logger;

        foreach (var indicatorStatus in IndicatorStatuses)
        {
            var symbolName = Enum.GetName(typeof(Symbol), indicatorStatus.Symbol);
            var pipSizeValue = configuration.GetSection("PipsValue")[symbolName!];
            if (double.TryParse(pipSizeValue, out var pipSize))
            {
                indicatorStatus.PipSize = pipSize;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    } 

    public ObservableCollection<IndicatorStatus> IndicatorStatuses
    {
        get;
    } = new(Enumerable.Range(0, TotalIndicators).Select(i => new IndicatorStatus { Index = i, IsConnected = false }));

    public bool IndicatorsConnected
    {
        get
        {
            for (var index = 0; index < TotalIndicators; index++)
            {
                if (IndicatorStatuses[index].IsConnected)
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }

    public Workplace Workplace
    {
        get
        {
            var result = IndicatorStatuses[0].Workplace;
            for (var index = 1; index < TotalIndicators; index++)
            {
                if (result != IndicatorStatuses[index].Workplace)
                {
                    return Workplace.None;
                }
            }

            return result;
        }
    }

    public void SetIndicator(Symbol symbol, DateTime dateTime, double ask, double bid, int counter)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var index = (int)symbol - 1;
            IndicatorStatuses[index].DateTime = dateTime;
            IndicatorStatuses[index].Ask = ask;
            IndicatorStatuses[index].Bid = bid;
            IndicatorStatuses[index].Counter = counter;
            OnPropertyChanged(nameof(IndicatorStatuses));
        });
    }

    public async Task IndicatorConnectedAsync(Symbol symbol, Workplace workplace)
    {
        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var index = (int)symbol - 1;
            IndicatorStatuses[index].IsConnected = true;
            IndicatorStatuses[index].Workplace = workplace;
            OnPropertyChanged(nameof(IndicatorStatuses));
        }).ConfigureAwait(true);

        if (!IndicatorsConnected)
        {
            return;
        }

        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            OnPropertyChanged(nameof(IndicatorsConnected));
        }).ConfigureAwait(true);

        InitializationComplete.Invoke();

        _logger.Log(LogLevel.Trace, "{TypeName}.{Guid}: Indicators connected.", GetType().Name, _guid);
    }

    public async Task IndicatorDisconnectedAsync(DeInitReason reason, int ticksSaved)
    {
        _reason = reason;
        _ticksSaved = ticksSaved;

        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            for (var index = 0; index < TotalIndicators; index++)
            {
                IndicatorStatuses[index].IsConnected = false;
                IndicatorStatuses[index].Workplace = Workplace.None;
            }

            OnPropertyChanged(nameof(IndicatorStatuses));
            OnPropertyChanged(nameof(IndicatorsConnected));
        }).ConfigureAwait(true);

        _logger.Log(LogLevel.Trace, "Indicators disconnected. Reason:{reason}. Ticks to be saved: {ticksSaved}.", _reason, _ticksSaved);
    }

    public void CloseApp(string message)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            _logger.Log(LogLevel.Critical, "{message}", message);
            _cts.Cancel();
            Task.Delay(2000);
            Environment.Exit(0);
        }).ConfigureAwait(true);
    }
}