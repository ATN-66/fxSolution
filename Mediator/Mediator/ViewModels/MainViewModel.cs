/*+------------------------------------------------------------------+
  |                                               Mediator.ViewModels|
  |                                                 MainViewModel.cs |
  +------------------------------------------------------------------+*/

using CommunityToolkit.Mvvm.ComponentModel;
using Mediator.Contracts.Services;
using System.Collections.ObjectModel;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Mediator.Models;
using Microsoft.Extensions.Logging;
using Symbol = Common.Entities.Symbol;
using Microsoft.Extensions.Configuration;
using CommunityToolkit.Mvvm.Input;
using Enum = System.Enum;
using Mediator.Helpers;
using Microsoft.UI.Xaml;
using Serilog;

namespace Mediator.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly IAppNotificationService _appNotificationService;
    private readonly IDataService _dataService;
    private readonly CancellationTokenSource _cts;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDataProviderService _dataProviderService;
    private readonly ILogger<MainViewModel> _logger;
    
    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    private Workplace _workplace;
    public event Action InitializationComplete = null!;

    public MainViewModel(IConfiguration configuration, IAppNotificationService appNotificationService, IDataService dataService, IDataProviderService dataProviderService, IDispatcherService dispatcherService, CancellationTokenSource cts, ILogger<MainViewModel> logger)
    {
        _dataService = dataService;
        _appNotificationService = appNotificationService;
        _cts = cts;
        _dispatcherService = dispatcherService;
        _dataProviderService = dataProviderService;
        _dataProviderService.IsServiceActivatedChanged += (_, e) => { IsDataProviderActivated = e.IsActivated; };
        _dataProviderService.IsClientActivatedChanged += (_, e) => { IsDataClientActivated = e.IsActivated; };

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
        private set
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
        private set
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

    public Workplace Workplace
    {
        get => _workplace;
        private set
        {
            if (value == _workplace)
            {
                return;
            }

            _workplace = value;
            OnPropertyChanged();
        }
    }

    private void CheckIndicatorsWorkplaces()//todo
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
        var result = await _dataService.BackupAsync().ConfigureAwait(true);
        _appNotificationService.ShowBackupResultToast(result);
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