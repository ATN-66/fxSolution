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
using CommunityToolkit.Mvvm.Input;
using Enum = System.Enum;
using Mediator.Helpers;
using Microsoft.UI.Xaml;
using Serilog;
using Mediator.Services;

namespace Mediator.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly IAppNotificationService _appNotificationService;
    private readonly CancellationTokenSource _cts;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<MainViewModel> _logger;
    
    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    private Workplace _workplace;
    private string _dataProviderServiceTitle = null!;
    public event Action InitializationComplete = null!;

    public MainViewModel(IConfiguration configuration, IAppNotificationService appNotificationService, IDispatcherService dispatcherService, CancellationTokenSource cts, ILogger<MainViewModel> logger)
    {
        _appNotificationService = appNotificationService;
        _cts = cts;
        _dispatcherService = dispatcherService;
        _logger = logger;

        Workplace = EnvironmentHelper.SetWorkplaceFromEnvironment();

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

        _logger.LogTrace("({Guid}) is ON.", _guid);
    }

    private bool? _isDataProviderActivated;
    public bool? IsDataProviderActivated
    {
        get => _isDataProviderActivated;
        internal set
        {
            if (value == _isDataProviderActivated)
            {
                return;
            }

            _isDataProviderActivated = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged(nameof(IsDataProviderActivated));
            }).ConfigureAwait(true);

        }
    }

    private bool _isDataClientActivated;
    public bool IsDataClientActivated
    {
        get => _isDataClientActivated;
        internal set
        {
            if (value == _isDataClientActivated)
            {
                return;
            }

            _isDataClientActivated = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged(nameof(IsDataClientActivated));
            }).ConfigureAwait(true);
        }
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
            
    public string DataProviderServiceTitle
    {
        get => _dataProviderServiceTitle;
        private set
        {
            if (value == _dataProviderServiceTitle)
            {
                return;
            }

            _dataProviderServiceTitle = value;
            OnPropertyChanged();
        }
    }

    private Workplace Workplace
    {
        get => _workplace;
        set
        {
            if (value == _workplace)
            {
                return;
            }

            _workplace = value;
            DataProviderServiceTitle = $"Data Provider Service ({Workplace})";
        }
    }

    private void CheckIndicatorsWorkplaces()
    {
        var result = Workplace;
        for (var index = 0; index < TotalIndicators; index++)
        {
            if (result != IndicatorStatuses[index].Workplace)
            {
                throw new InvalidOperationException($"Indicators workplaces don't match {EnvironmentHelper.GetExecutingAssemblyName()} workplace.");
            }
        }
    }

    public void SetIndicator(Quotation quotation, int counter)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var index = (int)quotation.Symbol - 1;
            IndicatorStatuses[index].DateTime = quotation.DateTime;
            IndicatorStatuses[index].Ask = quotation.Ask;
            IndicatorStatuses[index].Bid = quotation.Bid;
            IndicatorStatuses[index].Counter = counter;
            IndicatorStatuses[index].Pulse = true;
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

        CheckIndicatorsWorkplaces();

        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            OnPropertyChanged(nameof(IndicatorsConnected));
        }).ConfigureAwait(true);

        const string message = "Indicators connected.";
        _appNotificationService.ShowMessage(message);
        _logger.LogTrace(message);

        InitializationComplete.Invoke();
    }

    public async Task IndicatorDisconnectedAsync(DeInitReason reason, int ticksToBeSaved)
    {
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

        Workplace = EnvironmentHelper.SetWorkplaceFromEnvironment(); 

        _appNotificationService.ShowMessage($"Indicators disconnected. Reason:{reason}. Ticks to be saved: {ticksToBeSaved}.");
        _logger.LogTrace("Indicators disconnected. Reason:{reason}. Ticks to be saved: {ticksToBeSaved}.", reason, ticksToBeSaved);
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        throw new NotImplementedException();

        //var result = await _dataService.BackupAsync().ConfigureAwait(true);
        //_appNotificationService.ShowBackupResultToast(result);
    }

    private void CloseApplication()
    {
        throw new NotImplementedException();
        //save all quotations and close all
        _cts.Cancel();
        Log.CloseAndFlush();
        Application.Current.Exit();
    }
}