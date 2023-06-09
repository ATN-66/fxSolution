/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
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
    private readonly HashSet<DateTime> _excludedDates;

    public TicksOverviewViewModel(IDataService dataService, IConfiguration configuration, IAppNotificationService notificationService, IDialogService dialogService, IDispatcherService dispatcherService)
    {
        _dataService = dataService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _dispatcherService = dispatcherService;

        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);

        UpdateTicksContributionsCommand = new AsyncRelayCommand(UpdateTicksContributionsAsync);
        ImportTicksCommand = new AsyncRelayCommand(ImportTicksAsync);
    }

    public IAsyncRelayCommand UpdateTicksContributionsCommand
    {
        get;
    }

    public IAsyncRelayCommand ImportTicksCommand
    {
        get;
    }

    public async void OnNavigatedTo(object parameter)
    {
        var contributions = await _dataService.GetAllTicksContributionsAsync().ConfigureAwait(true);
        UpdateGroups(contributions);
    }

    public void OnNavigatedFrom()
    {
        _cts?.Cancel();
        Messenger.Unregister<DataChangedMessage, Info>(this, Info.Ticks);
    }

    private void UpdateGroups(IEnumerable<HourlyContribution> contributions)
    {
        Groups.Clear();

        var groupedByYear = contributions
            .GroupBy(c => c.DateTime.Year)
            .Select(g => new
            {
                Year = g.Key,
                Months = g.GroupBy(c => c.DateTime.Month)
                    .Select(m => new
                    {
                        Month = m.Key,
                        Days = m.GroupBy(c => c.DateTime.Day)
                    })
            });

        foreach (var yearGroup in groupedByYear)
        {
            var yearlyContribution = new YearlyContribution
            {
                Year = yearGroup.Year,
                MonthlyContributions = new ObservableCollection<MonthlyContribution>()
            };

            foreach (var monthGroup in yearGroup.Months)
            {
                var monthlyContribution = new MonthlyContribution
                {
                    Year = yearGroup.Year,
                    Month = monthGroup.Month,
                    DailyContributions = new ObservableCollection<DailyContribution>()
                };

                foreach (var dayGroup in monthGroup.Days)
                {
                    var hourlyContributions = dayGroup.ToList();
                    var dailyContribution = new DailyContribution
                    {
                        Year = yearGroup.Year,
                        Month = monthGroup.Month,
                        Day = hourlyContributions[0].Day,
                        Contribution = DetermineContributionStatus(hourlyContributions),
                        HourlyContributions = hourlyContributions
                    };

                    monthlyContribution.DailyContributions.Add(dailyContribution);
                }

                yearlyContribution.MonthlyContributions.Add(monthlyContribution);
            }

            Groups.Add(yearlyContribution);
        }
    }

    private Contribution DetermineContributionStatus(IReadOnlyList<HourlyContribution> hourlyContributions)
    {
        if (_excludedDates.Contains(hourlyContributions[0].DateTime.Date) || hourlyContributions[0].DateTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return Contribution.Excluded;
        }

        switch (hourlyContributions[0].DateTime.DayOfWeek)
        {
            case DayOfWeek.Friday when hourlyContributions.SkipLast(2).All(c => c.HasContribution):
            case DayOfWeek.Sunday when hourlyContributions.TakeLast(2).All(c => c.HasContribution):
                return Contribution.Full;
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Saturday:
            default:
            {
                if (hourlyContributions.All(c => c.HasContribution))
                {
                    return Contribution.Full;
                }

                return hourlyContributions.Any(c => c.HasContribution) ? Contribution.Partial : Contribution.None;
            }
        }
    }

    private async Task UpdateTicksContributionsAsync()
    {
        var result = await _dialogService.ShowDialogAsync("title", "Save your work?", "Save", "Don't Save", "Cancel");

        //if (!proceed)
        //{
        //    return;
        //}

        Messenger.Register<TicksOverviewViewModel, DataChangedMessage, Info>(this, Info.Ticks, (_, m) =>
        {
            OnDataServiceMessageReceived(m);
        });

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.UpdateTicksContributionsAsync(_cts.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException e)
            {
                _notificationService.Show($"Operation cancelled:{e.Message}");
            }
            finally
            {
                Messenger.Unregister<DataChangedMessage, Info>(this, Info.Ticks);
            }
        }
    }

    private async Task ImportTicksAsync()
    {
        var filteredGroups = FilterGroups();

        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.ImportTicksAsync(filteredGroups, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                _notificationService.Show($"Operation cancelled:{e.Message}");
            }
        }
    }

    private List<YearlyContribution> FilterGroups()
    {
        throw new NotImplementedException();

        //var filteredGroups = Groups
        //    .Select(y => new YearlyContribution
        //    {
        //        Year = y.Year,
        //        MonthlyContributions = y.MonthlyContributions
        //            .Select(m => new MonthlyContribution
        //            {
        //                Month = m.Month,
        //                DailyContributions = m.DailyContributions
        //                    .Where(d => !_excludedDates.Contains(d.DateTime.Date) && d.Contribution != Contribution.Excluded && d.Contribution != Contribution.Full)
        //                    .Select(d => new DailyContribution
        //                    {
        //                        DateTime = d.DateTime,
        //                        Contribution = d.Contribution,
        //                        HourlyContributions = d.HourlyContributions
        //                            .Where(h => !h.HasContribution)
        //                            .ToList()
        //                    })
        //                    .ToList()
        //            })
        //            .ToList()
        //    })
        //    .ToList();
        //return filteredGroups;
    }

    private void OnDataServiceMessageReceived(DataChangedMessage message)
    {
        Debug.Assert(_dispatcherService.IsDispatcherQueueHasThreadAccess);
        var dailyContribution = message.Value;
        var yearlyContribution = Groups.FirstOrDefault(y => y.Year == dailyContribution.Year);
        var monthlyContribution = yearlyContribution!.MonthlyContributions!.FirstOrDefault(m => m.Month == dailyContribution.Month);
        var existingDailyContribution = monthlyContribution!.DailyContributions.FirstOrDefault(d => d.Day == dailyContribution.Day);
        var index = monthlyContribution.DailyContributions.IndexOf(existingDailyContribution!);
        monthlyContribution.DailyContributions[index] = dailyContribution;
        monthlyContribution.DailyContributions[index].Contribution = DetermineContributionStatus(monthlyContribution.DailyContributions[index].HourlyContributions);
    }
}