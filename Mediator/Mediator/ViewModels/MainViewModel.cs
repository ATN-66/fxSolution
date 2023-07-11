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
using Enum = System.Enum;
using Mediator.Helpers;
using Windows.Web.AtomPub;

namespace Mediator.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private readonly Guid _guid = Guid.NewGuid();
    private readonly IAppNotificationService _appNotificationService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<MainViewModel> _logger;
    
    private static readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    private Workplace _workplace;

    private ServiceStatus _dataProviderStatus;
    private ServiceStatus _executiveProviderStatus;
    private ClientStatus _dataClientStatus;
    private ClientStatus _executiveClientStatus;
  
    private string _dataProviderServiceTitle = null!;
    private string _executiveProviderServiceTitle = null!;

    public event Action InitializationComplete = null!;
    private bool _connecting;
    private bool _atFault;

    public MainViewModel(IConfiguration configuration, IAppNotificationService appNotificationService, IDispatcherService dispatcherService, ILogger<MainViewModel> logger)
    {
        _appNotificationService = appNotificationService;
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

        _dataProviderStatus = ServiceStatus.Off;
        _executiveProviderStatus = ServiceStatus.Off;
        _dataClientStatus = ClientStatus.Off;
        _executiveClientStatus = ClientStatus.Off;

        _logger.LogTrace("({Guid}) is ON.", _guid);
    }
    
    public ServiceStatus DataProviderStatus
    {
        get => _dataProviderStatus;
        internal set
        {
            if (value == _dataProviderStatus)
            {
                return;
            }

            _dataProviderStatus = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged();
            }).ConfigureAwait(true);
        }
    }
    public ServiceStatus ExecutiveProviderStatus
    {
        get => _executiveProviderStatus;
        internal set
        {
            if (value == _executiveProviderStatus)
            {
                return;
            }

            _executiveProviderStatus = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged();
            }).ConfigureAwait(true);
        }
    }
    public ClientStatus DataClientStatus
    {
        get => _dataClientStatus;
        internal set
        {
            if (value == _dataClientStatus)
            {
                return;
            }

            _dataClientStatus = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged();
            }).ConfigureAwait(true);
        }
    }
    public ClientStatus ExecutiveClientStatus
    {
        get => _executiveClientStatus;
        internal set
        {
            if (value == _executiveClientStatus)
            {
                return;
            }

            _executiveClientStatus = value;
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                OnPropertyChanged();
            }).ConfigureAwait(true);
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

            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                _dataProviderServiceTitle = value;
                OnPropertyChanged();
            });
        }
    }
    public string ExecutiveProviderServiceTitle
    {
        get => _executiveProviderServiceTitle;
        private set
        {
            if (value == _executiveProviderServiceTitle)
            {
                return;
            }

            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                _executiveProviderServiceTitle = value;
                OnPropertyChanged();
            });
        }
    }

    public ObservableCollection<IndicatorStatus> IndicatorStatuses
    {
        get;
    } = new(Enumerable.Range(0, TotalIndicators).Select(i => new IndicatorStatus { Index = i, IsConnected = false }));

    public ConnectionStatus ConnectionStatus
    {
        get
        {
            var connectedCount = 0;
            for (var index = 0; index < TotalIndicators; index++)
            {
                if (IndicatorStatuses[index].IsConnected)
                {
                    connectedCount++;
                }
            }

            if (connectedCount == 0)
            {
                return ConnectionStatus.Disconnected;
            }

            if (connectedCount == TotalIndicators)
            {
                return ConnectionStatus.Connected;
            }

            return _connecting ? ConnectionStatus.Connecting : ConnectionStatus.Disconnecting;
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
            DataProviderServiceTitle = $"Data ({Workplace})";
            ExecutiveProviderServiceTitle = $"Execution ({Workplace})";
        }
    }

    public bool AtFault
    {
        get => _atFault;
        set =>
            _dispatcherService.ExecuteOnUIThreadAsync(() =>
            {
                DataProviderServiceTitle = "There's a malfunction with the service.";
                _atFault = value;
                OnPropertyChanged();
            });
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
            IndicatorStatuses[index].Pulse = !IndicatorStatuses[index].Pulse;
            OnPropertyChanged(nameof(IndicatorStatuses));
        });
    }

    public async Task IndicatorConnectedAsync(Symbol symbol, Workplace workplace)
    {
        _connecting = true;
        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var index = (int)symbol - 1;
            IndicatorStatuses[index].IsConnected = true;
            IndicatorStatuses[index].Workplace = workplace;
            OnPropertyChanged(nameof(IndicatorStatuses));
            OnPropertyChanged(nameof(ConnectionStatus));
        }).ConfigureAwait(true);

        if (ConnectionStatus != ConnectionStatus.Connected)
        {
            return;
        }

        CheckIndicatorsWorkplaces();
        const string message = "Indicators connected.";
        //_appNotificationService.ShowMessage(message);
        _logger.LogTrace(message);

        InitializationComplete.Invoke();
    }

    public async Task IndicatorDisconnectedAsync(Symbol symbol, DeInitReason reason)
    {
        _connecting = false;
        await _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var index = (int)symbol - 1;
            IndicatorStatuses[index].IsConnected = false;
            IndicatorStatuses[index].Workplace = Workplace.None;
            OnPropertyChanged(nameof(IndicatorStatuses));
            OnPropertyChanged(nameof(ConnectionStatus));
        }).ConfigureAwait(true);

        if (ConnectionStatus != ConnectionStatus.Disconnected)
        {
            return;
        }

        Workplace = EnvironmentHelper.SetWorkplaceFromEnvironment(); 

        //_appNotificationService.ShowMessage($"Indicators disconnected. Reason:{reason}.");
        _logger.LogTrace("Indicators disconnected. Reason:{reason}.", reason);
    }
}