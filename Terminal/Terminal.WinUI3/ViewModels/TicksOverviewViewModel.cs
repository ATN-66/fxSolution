/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
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

    [ObservableProperty] private ObservableCollection<YearlyContribution> _groups = new();
    [ObservableProperty] private string _headerContext = "Ticks";
    private CancellationTokenSource? _cts;
    private DialogViewModel _dialogViewModel = null!;

    public TicksOverviewViewModel(IDataService dataService, IConfiguration configuration, IAppNotificationService notificationService, IDialogService dialogService, IDispatcherService dispatcherService)
    {
        _dataService = dataService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _dispatcherService = dispatcherService;

        RecalculateTicksContributionsCommand = new AsyncRelayCommand(RecalculateTicksContributionsAsync);
        ImportTicksCommand = new AsyncRelayCommand(ImportTicksAsync);
    }

    public IAsyncRelayCommand RecalculateTicksContributionsCommand
    {
        get;
    }

    public IAsyncRelayCommand ImportTicksCommand
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
    }

    private async Task RefreshContributionsAsync()
    {
        var input = await _dataService.GetAllTicksContributionsAsync().ConfigureAwait(true);
        Groups.Clear();
        foreach (var yearlyContribution in input)
        {
            Groups.Add(yearlyContribution);
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
        throw new NotImplementedException();

        //var dialog = _dialogService.CreateDialog("Ticks Importations", "Importing... Please wait.", "Cancel", null, null);
        //var dialogTask = dialog.ShowAsync().AsTask();
        //var importTask = PerformImportTicksAsync();
        //var completedTask = await Task.WhenAny(dialogTask, importTask).ConfigureAwait(true);

        //if (completedTask == dialogTask && await dialogTask.ConfigureAwait(true) == ContentDialogResult.Primary)
        //{
        //    _cts?.Cancel();
        //}

        //if (completedTask == importTask)
        //{
        //    dialog.Hide();
        //}
    }

    private async Task PerformImportTicksAsync()
    {
        throw new NotImplementedException();

        //Messenger.Register<TicksOverviewViewModel, DataServiceMessage, DataServiceToken>(this, DataServiceToken.Ticks, (_, m) =>
        //{
        //    OnDataServiceMessageReceived(m);
        //});

        //using (_cts = new CancellationTokenSource())
        //{
        //    try
        //    {
        //        await _dataService.ImportTicksAsync(_cts.Token).ConfigureAwait(true);
        //    }
        //    catch (OperationCanceledException e)
        //    {
        //        _notificationService.Show($"Operation cancelled:{e.Message}");
        //    }
        //    finally
        //    {
        //        Messenger.Unregister<DataServiceMessage, DataServiceToken>(this, DataServiceToken.Ticks);
        //    }
        //}
    }

    private void OnDailyContributionChanged(DailyContribution dailyContribution)
    {
        Debug.Assert(_dispatcherService.IsDispatcherQueueHasThreadAccess);
        var yearlyContribution = Groups.FirstOrDefault(y => y.Year == dailyContribution.Year);
        var monthlyContribution = yearlyContribution!.MonthlyContributions.FirstOrDefault(m => m.Month == dailyContribution.Month);
        var existingDailyContribution = monthlyContribution!.DailyContributions.FirstOrDefault(d => d.Day == dailyContribution.Day);
        var index = monthlyContribution.DailyContributions.IndexOf(existingDailyContribution!);
        monthlyContribution.DailyContributions[index] = dailyContribution;
    }

    private void OnProgressReported(int progressPercentage)
    {
        _dialogViewModel.ProgressPercentage = progressPercentage;
    }
}