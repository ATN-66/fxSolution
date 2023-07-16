/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksContributionsViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksContributionsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly IAppNotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IDispatcherService _dispatcherService;//todo:remove later

    [ObservableProperty] private string _headerContext = "Ticks";

    public List<Common.Entities.Symbol> Symbols { get; } = Enum.GetValues(typeof(Common.Entities.Symbol)).Cast<Common.Entities.Symbol>().ToList();
    [ObservableProperty] private Common.Entities.Symbol _selectedSymbol;
    [ObservableProperty] private DateTimeOffset _selectedDate;
    [ObservableProperty] private TimeSpan _selectedTime;
    private DateTime _currentDate;

    [ObservableProperty] private BulkObservableCollection<YearlyContribution> _yearlyContributions = new();
    [ObservableProperty] private bool _yearlyContributionsIsLoading;
    [ObservableProperty] private int _yearlyContributionsCount;

    [ObservableProperty] private BulkObservableCollection<DailyBySymbolContribution> _hourlyContributions = new();
    [ObservableProperty] private bool _hourlyContributionsIsLoading;
    [ObservableProperty] private int _hourlyContributionsCount;
    
    [ObservableProperty] private BulkObservableCollection<Quotation> _fileServiceQuotations = new();
    [ObservableProperty] private bool _fileServiceQuotationsIsLoading;
    [ObservableProperty] private int _fileServiceQuotationsCount;

    [ObservableProperty] private BulkObservableCollection<Quotation> _terminalQuotations = new();
    [ObservableProperty] private bool _terminalQuotationsIsLoading;
    [ObservableProperty] private int _terminalQuotationsCount;

    [ObservableProperty] private BulkObservableCollection<Quotation> _mediatorQuotations = new();
    [ObservableProperty] private bool _mediatorQuotationsIsLoading;
    [ObservableProperty] private int _mediatorQuotationsCount;

    [ObservableProperty] private Provider _selectedProvider;
    public IEnumerable<Provider> Providers
    {
        get;
    }

    private CancellationTokenSource? _cts;
    private DialogViewModel _dialogViewModel = null!;
    public DateTimeOffset Today { get; } = DateTimeOffset.Now;

    public TicksContributionsViewModel(IDataService dataService, IDialogService dialogService, IAppNotificationService notificationService, IDispatcherService dispatcherService)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _dispatcherService = dispatcherService;

        _selectedDate = Today;
        Providers = Enum.GetValues(typeof(Provider)).Cast<Provider>().Where(p => p != Provider.Terminal);
    }

    partial void OnSelectedDateChanged(DateTimeOffset oldValue, DateTimeOffset newValue)
    {
        if (oldValue.Equals(newValue))
        {
            return;
        }

        SelectedTime = TimeSpan.Zero;
        ResetCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTimeChanged(TimeSpan value)
    {
        if (value.Equals(SelectedTime))
        {
            return;
        }

        ResetCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteReset()
    {
        return SelectedDate.DateTime.Date != Today.DateTime.Date || SelectedTime != TimeSpan.Zero;
    }

    public void OnNavigatedTo(object parameter)
    {
        _ = RefreshContributionsAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
        Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
        Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
    }

    private async Task RefreshContributionsAsync()
    {
        SelectedProvider = Provider.Mediator;
        YearlyContributionsIsLoading = true;
        YearlyContributionsCount = 0;
        YearlyContributions.Clear();
        var yearlyContributions = await _dataService.GetYearlyContributionsAsync().ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasUIThreadAccess);
        YearlyContributions.AddRange(yearlyContributions);
        YearlyContributionsCount = YearlyContributions.Count;
        YearlyContributionsIsLoading = false;
    }

    [RelayCommand]
    private Task ContributionsAsync()
    {
        var hoursTask = GetHoursAsync();
        var ticksTask = GetTicksAsync();

        return Task.WhenAll(hoursTask, ticksTask);
    }

    private async Task GetHoursAsync(bool inforce = false)
    {
        if (_currentDate == SelectedDate.DateTime.Date && inforce == false)
        {
            return;
        }

        HourlyContributionsIsLoading = true;
        HourlyContributionsCount = 0;
        HourlyContributions.Clear();

        var contributions = await _dataService.GetDayContributionAsync(SelectedDate.DateTime.Date).ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasUIThreadAccess);
        HourlyContributions.AddRange(contributions);

        _currentDate = SelectedDate.DateTime.Date;
        HourlyContributionsCount = HourlyContributions.Count;
        HourlyContributionsIsLoading = false;
    }

    private async Task GetTicksAsync()
    {
        var dateTimeInclusive = SelectedDate.Date.AddHours(SelectedTime.Hours);
        var endDateTimeInclusive = SelectedDate.Date.AddHours(SelectedTime.Hours);

        var fileServiceTask = _dataService.GetHistoricalDataAsync(SelectedSymbol, dateTimeInclusive, endDateTimeInclusive, Provider.FileService);
        var mediatorTask = _dataService.GetHistoricalDataAsync(SelectedSymbol, dateTimeInclusive, endDateTimeInclusive, Provider.Mediator);
        var terminalTask = _dataService.GetHistoricalDataAsync(SelectedSymbol, dateTimeInclusive, endDateTimeInclusive, Provider.Terminal);

        FileServiceQuotationsIsLoading = true;
        MediatorQuotationsIsLoading = true;
        TerminalQuotationsIsLoading = true;
        FileServiceQuotationsCount = 0;
        MediatorQuotationsCount = 0;
        TerminalQuotationsCount = 0;
        
        await Task.WhenAll(tasks: new[] { fileServiceTask, mediatorTask, terminalTask }).ConfigureAwait(true);

        var fileServiceResult = await fileServiceTask.ConfigureAwait(true);
        FileServiceQuotations.AddRange(fileServiceResult.ToList());
        FileServiceQuotationsCount = FileServiceQuotations.Count;
        FileServiceQuotationsIsLoading = false;

        var mediatorResult = await mediatorTask.ConfigureAwait(true);
        MediatorQuotations.AddRange(mediatorResult.ToList());
        MediatorQuotationsCount = MediatorQuotations.Count;
        MediatorQuotationsIsLoading = false;

        var terminalResult = await terminalTask.ConfigureAwait(true);
        TerminalQuotations.AddRange(terminalResult.ToList());
        TerminalQuotationsCount = TerminalQuotations.Count;
        TerminalQuotationsIsLoading = false;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReset))]
    private void Reset()
    {
        SelectedDate = Today;
        SelectedTime = TimeSpan.Zero;
        SelectedSymbol = Symbols[0];

        HourlyContributions.Clear();
        HourlyContributionsCount = HourlyContributions.Count;

        FileServiceQuotations.Clear();
        FileServiceQuotationsCount = FileServiceQuotations.Count;

        MediatorQuotations.Clear();
        MediatorQuotationsCount = FileServiceQuotations.Count;

        TerminalQuotations.Clear();
        TerminalQuotationsCount = FileServiceQuotations.Count;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        Reset();

        _dialogViewModel = new DialogViewModel
        {
            CautionMessage = "Importing... Please wait."
        };
        var dialog = _dialogService.CreateDialog(_dialogViewModel, "Ticks Import", "Cancel", null, null);
        var dialogTask = dialog.ShowAsync().AsTask();
        var updateTask = PerformImportAsync();
        var completedTask = await Task.WhenAny(dialogTask, updateTask).ConfigureAwait(true);
        if (completedTask == dialogTask && await dialogTask.ConfigureAwait(true) == ContentDialogResult.Primary)
        {
            _cts?.Cancel();
        }

        if (completedTask == updateTask)
        {
            dialog.Hide();
        }

        _ = RefreshContributionsAsync().ConfigureAwait(true);
    }

    private async Task PerformImportAsync()
    {
        if (!Messenger.IsRegistered<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate))
        {
            Messenger.Register<TicksContributionsViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
            {
                OnDailyContributionChanged(m.Value);
            });
        }

        if (!Messenger.IsRegistered<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress))
        {
            Messenger.Register<TicksContributionsViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
            {
                OnProgressReported(m.Value);
            });
        }

        if (!Messenger.IsRegistered<InfoMessage, DataServiceToken>(this, DataServiceToken.Info))
        {
            Messenger.Register<TicksContributionsViewModel, InfoMessage, DataServiceToken>(this, DataServiceToken.Info, (_, m) =>
            {
                OnInfoReported(m.Value);
            });
        }

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.ImportAsync(_cts.Token, SelectedProvider).ConfigureAwait(true);
            }
            catch (OperationCanceledException e)
            {
                _notificationService.Show($"Position cancelled:{e.Message}");
            }
            finally
            {
                Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
                Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
            }
        }
    }

    [RelayCommand]
    private async Task ReImportSelectedAsync()
    {
        HourlyContributionsIsLoading = true;
        HourlyContributionsCount = 0;
        HourlyContributions.Clear();

        var result = await _dataService.ReImportSelectedAsync(SelectedDate.DateTime.Date, SelectedProvider).ConfigureAwait(true);
        Debug.WriteLine(result);
        _ = RefreshContributionsAsync().ConfigureAwait(true);
        _ = GetHoursAsync(true).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RecalculateAllContributionsAsync()
    {
        Reset();

        _dialogViewModel = new DialogViewModel
        {
            CautionMessage = "Recalculating all... Please wait."
        };
        var dialog = _dialogService.CreateDialog(_dialogViewModel, "Contributions Recalculating", "Cancel", null, null);
        var dialogTask = dialog.ShowAsync().AsTask();
        var updateTask = PerformRecalculatingAllContributionsAsync();
        var completedTask = await Task.WhenAny(dialogTask, updateTask).ConfigureAwait(true);
        if (completedTask == dialogTask && await dialogTask.ConfigureAwait(true) == ContentDialogResult.Primary)
        {
            _cts?.Cancel();
        }

        if (completedTask == updateTask)
        {
            dialog.Hide();
        }

        _ = RefreshContributionsAsync().ConfigureAwait(true);
    }

    private async Task PerformRecalculatingAllContributionsAsync()
    {
        if (!Messenger.IsRegistered<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate))
        {
            Messenger.Register<TicksContributionsViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
            {
                OnDailyContributionChanged(m.Value);
            });
        }

        if (!Messenger.IsRegistered<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress))
        {
            Messenger.Register<TicksContributionsViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
            {
                OnProgressReported(m.Value);
            });
        }

        if (!Messenger.IsRegistered<InfoMessage, DataServiceToken>(this, DataServiceToken.Info))
        {
            Messenger.Register<TicksContributionsViewModel, InfoMessage, DataServiceToken>(this, DataServiceToken.Info, (_, m) =>
            {
                OnInfoReported(m.Value);
            });
        }

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.RecalculateAllContributionsAsync(_cts.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException e)
            {
                _notificationService.Show($"Position cancelled:{e.Message}");
            }
            finally
            {
                Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
                Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
            }
        }
    }

    //[RelayCommand]
    //private async Task BackupAsync()
    //{
    //    var result = await _dataService.BackupAsync().ConfigureAwait(true);
    //    Debug.WriteLine(result);//todo: notify Contribution
    //}

    //[RelayCommand]
    //private async Task RestoreAsync()
    //{
    //    var result = await _dataService.RestoreAsync().ConfigureAwait(true);
    //    Debug.WriteLine(result);//todo: notify Contribution
    //}

    private void OnDailyContributionChanged(DailyContribution dailyContribution)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            var yearlyContribution = YearlyContributions.FirstOrDefault(y => y.Year == dailyContribution.Year);
            var monthlyContribution = yearlyContribution!.MonthlyContributions!.FirstOrDefault(m => m.Month == dailyContribution.Month);
            var existingDailyContribution = monthlyContribution!.DailyContributions.FirstOrDefault(d => d.Day == dailyContribution.Day);
            var index = monthlyContribution.DailyContributions.IndexOf(existingDailyContribution!);
            monthlyContribution.DailyContributions[index] = dailyContribution;
        });
    }

    private void OnProgressReported(int progressPercentage)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            _dialogViewModel.ProgressPercentage = progressPercentage;
        });
    }

    private void OnInfoReported(string info)
    {
        _dispatcherService.ExecuteOnUIThreadAsync(() =>
        {
            _dialogViewModel.InfoMessage = info;
        });
    }
}