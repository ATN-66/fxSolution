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
using Grpc.Core;

namespace Mediator.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly IConfiguration _configuration;
    private readonly IAppNotificationService _appNotificationService;
    private readonly IDataService _dataService;
    private readonly CancellationTokenSource _cts;
    private readonly IDispatcherService _dispatcherService;
    private readonly ITicksDataProviderService _ticksDataProviderService;
    private readonly ILogger<MainViewModel> _logger;
    
    private DeInitReason _reason;
    private int _ticksSaved;
    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    private Workplace _workplace;
    public event Action InitializationComplete = null!;

    public MainViewModel(IConfiguration configuration, IAppNotificationService appNotificationService, IDataService dataService, ITicksDataProviderService ticksDataProviderService, IDispatcherService dispatcherService, CancellationTokenSource cts, ILogger<MainViewModel> logger)
    {
        _configuration = configuration;
        _dataService = dataService;
        _appNotificationService = appNotificationService;
        _cts = cts;
        _dispatcherService = dispatcherService;
        _ticksDataProviderService = ticksDataProviderService;
        //IsTicksDataProviderServiceActivated = _ticksDataProviderService.IsActivated;
        _ticksDataProviderService.IsActivatedChanged += (s, e) =>
        {
            IsTicksDataProviderServiceActivated = e.IsActivated;
        };

        _logger = logger;

        Workplace = SetWorkplaceFromEnvironment();

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

        _logger.Log(LogLevel.Trace, "{TypeName}.{Guid} is ON.", GetType().Name, _guid);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    }

    private bool _isTicksDataProviderServiceActivated;
    public bool IsTicksDataProviderServiceActivated
    {
        get => _isTicksDataProviderServiceActivated;
        private set
        {
            if (value == _isTicksDataProviderServiceActivated)
            {
                return;
            }

            _isTicksDataProviderServiceActivated = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged(nameof(IsTicksDataProviderServiceActivated));
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
            _dataService.Workplace = Workplace;
            OnPropertyChanged();
        }
    }

    private Workplace SetWorkplaceFromIndicators()
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

    private Workplace SetWorkplaceFromEnvironment()
    {
        string computerDevelopment;
        computerDevelopment = _configuration.GetValue<string>($"{nameof(computerDevelopment)}")!;
        string computerProduction;
        computerProduction = _configuration.GetValue<string>($"{nameof(computerProduction)}")!;

        if (Environment.MachineName == computerDevelopment)
        {
            return Workplace.Development;
        }

        if (Environment.MachineName == computerProduction)
        {
            return Workplace.Production;
        }

        throw new InvalidOperationException($"{nameof(Environment.MachineName)}");
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


        Workplace = SetWorkplaceFromIndicators();
        _appNotificationService.ShowMessage("Indicators connected");

        _logger.Log(LogLevel.Trace, "{TypeName}.{Guid}: Indicators connected.", GetType().Name, _guid);
        InitializationComplete.Invoke();
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

        Workplace = SetWorkplaceFromEnvironment(); 
        _appNotificationService.ShowMessage($"Indicators disconnected. Ticks to be saved: {_ticksSaved}.");

        _logger.Log(LogLevel.Trace, "Indicators disconnected. Reason:{reason}. Ticks to be saved: {ticksSaved}.", _reason, _ticksSaved);
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        var result = await _dataService.BackupAsync().ConfigureAwait(true);
        _appNotificationService.ShowBackupResultToast(result);
    }

    private int _progressPercentage;
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (_progressPercentage == value)
            {
                return;
            }

            _progressPercentage = value;
            OnPropertyChanged();
        }
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