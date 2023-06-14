/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using System.Globalization;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;
using Windows.System;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
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

    [ObservableProperty] private BulkObservableCollection<SymbolicContribution> _hourlyContributions = new();
    [ObservableProperty] private bool _hourlyContributionsIsLoading;
    [ObservableProperty] private int _hourlyContributionsCount;
    
    [ObservableProperty] private BulkObservableCollection<Quotation> _quotations = new();
    [ObservableProperty] private bool _quotationsIsLoading;
    [ObservableProperty] private int _quotationsCount;
    
    private CancellationTokenSource? _cts;
    private DialogViewModel _dialogViewModel = null!;
    private readonly DateTimeOffset _startDateTimeOffset;

    public TicksOverviewViewModel(IDataService dataService, IDialogService dialogService, IAppNotificationService notificationService, IDispatcherService dispatcherService, IConfiguration configuration)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _dispatcherService = dispatcherService;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };
        _selectedDate = _startDateTimeOffset = DateTimeOffset.ParseExact(configuration.GetValue<string>("StartDate")!, formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
    }

    partial void OnSelectedDateChanged(DateTimeOffset oldValue, DateTimeOffset newValue)
    {
        SelectedTime = TimeSpan.Zero;
        RecalculateTicksContributionsSelectedDayCommand.NotifyCanExecuteChanged();
        ResetDateTimeCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteReset()
    {
        return SelectedDate.DateTime.Date != _startDateTimeOffset.DateTime.Date;
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
        YearlyContributionsIsLoading = true;
        YearlyContributionsCount = 0;
        YearlyContributions.Clear();
        var yearlyContributions = await _dataService.GetYearlyContributionsAsync().ConfigureAwait(true);
        YearlyContributions.AddRange(yearlyContributions);
        YearlyContributionsCount = YearlyContributions.Count;
        YearlyContributionsIsLoading = false;
    }

    [RelayCommand]
    private Task HourlyContributionsAsync()
    {
        var hoursTask = GetHoursAsync();
        var ticksTask = GetImportTicksAsync();

        return Task.WhenAll(hoursTask, ticksTask);
    }

    private async Task GetHoursAsync()
    {
        if (_currentDate == SelectedDate.DateTime.Date)
        {
            return;
        }

        HourlyContributionsIsLoading = true;
        HourlyContributionsCount = 0;
        HourlyContributions.Clear();

        var contributions = await _dataService.GetSymbolicContributionsAsync(SelectedDate).ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasThreadAccess);
        HourlyContributions.AddRange(contributions);

        _currentDate = SelectedDate.DateTime.Date;
        HourlyContributionsCount = HourlyContributions.Count;
        HourlyContributionsIsLoading = false;
    }

    private async Task GetImportTicksAsync()
    {
        QuotationsIsLoading = true;
        QuotationsCount = 0;
        Quotations.Clear();

        var startDateTime = SelectedDate.Date.AddHours(SelectedTime.Hours);
        var endDateTime = startDateTime.AddHours(1);

        var input = await _dataService.GetImportTicksAsync(SelectedSymbol, startDateTime, endDateTime).ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasThreadAccess);
        Quotations.AddRange(input.ToList());

        QuotationsCount = Quotations.Count;
        QuotationsIsLoading = false;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReset))]
    private void ResetDateTime()
    {
        SelectedSymbol = Symbols[0];
        SelectedDate = _startDateTimeOffset;
        SelectedTime = TimeSpan.Zero;
        HourlyContributions.Clear();
        HourlyContributionsCount = HourlyContributions.Count;
        Quotations.Clear();
        QuotationsCount = Quotations.Count;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReset))]
    private async Task RecalculateTicksContributionsSelectedDayAsync()
    {
        //_dialogViewModel = new DialogViewModel
        //{
        //    InfoMessage = "Recalculating Selected Date... Please wait."
        //};
        //var dialog = _dialogService.CreateDialog(_dialogViewModel, "Ticks Contributions", "Cancel", null, null);
        //var dialogTask = dialog.ShowAsync().AsTask();
        //var updateTask = PerformRecalculateTicksContributionsSelectedDayAsync(SelectedDate.Date);
        //var completedTask = await Task.WhenAny(dialogTask, updateTask).ConfigureAwait(true);
        //if (completedTask == dialogTask && await dialogTask.ConfigureAwait(true) == ContentDialogResult.Primary)
        //{
        //    _cts?.Cancel();
        //}

        //if (completedTask == updateTask)
        //{
        //    dialog.Hide();
        //}

        //_ = RefreshContributionsAsync().ConfigureAwait(true);
    }

    private async Task PerformRecalculateTicksContributionsSelectedDayAsync(DateTime dateTime)
    {
        //Messenger.Register<TicksOverviewViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
        //{
        //    OnDailyContributionChanged(m.Value);
        //});

        //Messenger.Register<TicksOverviewViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
        //{
        //    OnProgressReported(m.Value);
        //});

        //using (_cts = new CancellationTokenSource())
        //{
        //    try
        //    {
        //        await _dataService.RecalculateTicksContributionsSelectedDayAsync(dateTime, _cts.Token).ConfigureAwait(true);
        //    }
        //    catch (OperationCanceledException e)
        //    {
        //        _notificationService.Show($"Operation cancelled:{e.Message}");
        //    }
        //    finally
        //    {
        //        //Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
        //        //Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
        //    }
        //}
    }

    [RelayCommand]
    private async Task RecalculateTicksContributionsAllAsync()
    {
        //_dialogViewModel = new DialogViewModel
        //{
        //    InfoMessage = "Recalculating All... Please wait."
        //};
        //var dialog = _dialogService.CreateDialog(_dialogViewModel, "Ticks Contributions", "Cancel", null, null);
        //var dialogTask = dialog.ShowAsync().AsTask();
        //var recalculateTask = PerformRecalculateTicksContributionsAllAsync();
        //var completedTask = await Task.WhenAny(dialogTask, recalculateTask).ConfigureAwait(true);
        //if (completedTask == dialogTask && await dialogTask.ConfigureAwait(true) == ContentDialogResult.Primary)
        //{
        //    _cts?.Cancel();
        //}

        //if (completedTask == recalculateTask)
        //{
        //    dialog.Hide();
        //}

        //_ = RefreshContributionsAsync().ConfigureAwait(true);
    }

    private async Task PerformRecalculateTicksContributionsAllAsync()
    {
        //throw new NotImplementedException();

        //Messenger.Register<TicksOverviewViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
        //{
        //    OnDailyContributionChanged(m.Value);
        //});

        //Messenger.Register<TicksOverviewViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
        //{
        //    OnProgressReported(m.Value);
        //});

        //using (_cts = new CancellationTokenSource())
        //{
        //    try
        //    {
        //        await _dataService.RecalculateTicksContributionsAllAsync(_cts.Token).ConfigureAwait(true);
        //    }
        //    catch (OperationCanceledException e)
        //    {
        //        _notificationService.Show($"Operation cancelled:{e.Message}");
        //    }
        //    finally
        //    {
        //        Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
        //        Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
        //    }
        //}
    }

    [RelayCommand]
    private async Task ImportTicksAsync()
    {
        ResetDateTime();

        _dialogViewModel = new DialogViewModel
        {
            CautionMessage = "Importing... Please wait."
        };
        var dialog = _dialogService.CreateDialog(_dialogViewModel, "Ticks Import", "Cancel", null, null);
        var dialogTask = dialog.ShowAsync().AsTask();
        var updateTask = PerformImportTicksAsync();
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

    private async Task PerformImportTicksAsync()
    {
        Messenger.Register<TicksOverviewViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
        {
            OnDailyContributionChanged(m.Value);
        });

        Messenger.Register<TicksOverviewViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
        {
            OnProgressReported(m.Value);
        });

        Messenger.Register<TicksOverviewViewModel, InfoMessage, DataServiceToken>(this, DataServiceToken.Info, (_, m) =>
        {
            OnInfoReported(m.Value);
        });

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.ImportTicksAsync(_cts.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException e)
            {
                _notificationService.Show($"Operation cancelled:{e.Message}");
            }
            finally
            {
                Messenger.Unregister<DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate);
                Messenger.Unregister<ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress);
            }
        }
    }

    private void OnDailyContributionChanged(DailyContribution dailyContribution)
    {
        Debug.Assert(_dispatcherService.HasThreadAccess);
        var yearlyContribution = YearlyContributions.FirstOrDefault(y => y.Year == dailyContribution.Year);
        var monthlyContribution = yearlyContribution!.MonthlyContributions!.FirstOrDefault(m => m.Month == dailyContribution.Month);
        var existingDailyContribution = monthlyContribution!.DailyContributions.FirstOrDefault(d => d.Day == dailyContribution.Day);
        var index = monthlyContribution.DailyContributions.IndexOf(existingDailyContribution!);
        monthlyContribution.DailyContributions[index] = dailyContribution;
    }

    private void OnProgressReported(int progressPercentage)
    {
        _dialogViewModel.ProgressPercentage = progressPercentage;
    }

    private void OnInfoReported(string info)
    {
        _dialogViewModel.InfoMessage = info;
    }
}