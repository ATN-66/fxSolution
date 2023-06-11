/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly IAppNotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty] private ObservableCollection<YearlyContribution> _yearlyContributions = new();
    [ObservableProperty] private ObservableCollection<SymbolicContribution> _symbolicContributions = new();
    [ObservableProperty] private DateTimeOffset _selectedDate;//todo: ui is not updated!
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _headerContext = "Ticks";//todo: not is use?
    private CancellationTokenSource? _cts;
    private DialogViewModel _dialogViewModel = null!;

    public TicksOverviewViewModel(IDataService dataService, IDialogService dialogService, IAppNotificationService notificationService, IDispatcherService dispatcherService, IConfiguration configuration)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _dispatcherService = dispatcherService;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };
        _selectedDate = DateTimeOffset.ParseExact(configuration.GetValue<string>("StartDate")!, formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();

        RecalculateTicksContributionsCommand = new AsyncRelayCommand(RecalculateTicksContributionsAsync);
        ImportTicksCommand = new AsyncRelayCommand(ImportTicksAsync);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
    }

    public IAsyncRelayCommand RecalculateTicksContributionsCommand
    {
        get;
    }

    public IAsyncRelayCommand ImportTicksCommand
    {
        get;
    }

    public IAsyncRelayCommand SubmitCommand
    {
        get;
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
        var input = await _dataService.GetYearlyContributionsAsync().ConfigureAwait(true);
        YearlyContributions.Clear();
        foreach (var yearlyContribution in input)
        {
            YearlyContributions.Add(yearlyContribution);
        }
    }

    private async Task RecalculateTicksContributionsAsync()
    {
        _dialogViewModel = new DialogViewModel
        {
            InfoMessage = "Recalculating... Please wait."
        };
        var dialog = _dialogService.CreateDialog(_dialogViewModel, "Ticks Contributions", "Cancel", null, null);
        var dialogTask = dialog.ShowAsync().AsTask();
        var updateTask = PerformRecalculateTicksContributionsAsync();
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

    private async Task PerformRecalculateTicksContributionsAsync()
    {
        Messenger.Register<TicksOverviewViewModel, DailyContributionChangedMessage, DataServiceToken>(this, DataServiceToken.DataToUpdate, (_, m) =>
        {
            OnDailyContributionChanged(m.Value);
        });

        Messenger.Register<TicksOverviewViewModel, ProgressReportMessage, DataServiceToken>(this, DataServiceToken.Progress, (_, m) =>
        {
            OnProgressReported(m.Value);
        });

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.RecalculateTicksContributionsAsync(_cts.Token).ConfigureAwait(true);
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

    private async Task ImportTicksAsync()
    {
        _dialogViewModel = new DialogViewModel
        {
            InfoMessage = "Importing... Please wait."
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

    private async Task SubmitAsync()
    {
        IsLoading = true;
        var contributions = await _dataService.GetSymbolicContributionsAsync(SelectedDate).ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasThreadAccess);

        SymbolicContributions.Clear();
        foreach (var contribution in contributions)
        {
            SymbolicContributions.Add(contribution);
        }
        IsLoading = false;
    }
}