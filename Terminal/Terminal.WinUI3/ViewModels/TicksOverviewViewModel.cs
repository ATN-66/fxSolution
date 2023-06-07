/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly IDataService _dataService;
    //private readonly IDispatcherService _dispatcherService;
    [ObservableProperty] private ObservableCollection<YearlyContribution> _groups = new();

    public TicksOverviewViewModel(IDataService dataService, IDispatcherService dispatcherService)
    {
        _dataService = dataService;
        //_dispatcherService = dispatcherService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        var contributions = await _dataService.GetTicksContributionsAsync().ConfigureAwait(true);

        // Group contributions by year, then month, then day
        var groupedByYear = contributions
            .GroupBy(c => c.DateTime.Year)
            .Select(g => new
            {
                Year = g.Key,
                Months = g.GroupBy(c => c.DateTime.Month).Select(m => new
                {
                    Month = m.Key,
                    Days = m.GroupBy(c => c.DateTime.Day)
                })
            });

        // Iterate over each year
        foreach (var yearGroup in groupedByYear)
        {
            var yearlyContribution = new YearlyContribution()

            {
                Year = yearGroup.Year,
                MonthlyContributions = new List<MonthlyContribution>()
            };

            // Iterate over each month in the year
            foreach (var monthGroup in yearGroup.Months)
            {
                var monthlyContribution = new MonthlyContribution()
                {
                    Month = monthGroup.Month,
                    DailyContributions = new List<DailyContribution>()
                };

                // Iterate over each day in the month
                foreach (var dayGroup in monthGroup.Days)
                {
                    var dailyContributions = dayGroup.ToList();

                    // Determine the Contribution status for the day
                    Contribution contributionStatus;
                    if (dailyContributions.All(c => c.HasContribution))
                    {
                        contributionStatus = Contribution.Full;
                    }
                    else if (dailyContributions.Any(c => c.HasContribution))
                    {
                        contributionStatus = Contribution.Partial;
                    }
                    else
                    {
                        contributionStatus = Contribution.None;
                    }

                    var dailyContribution = new DailyContribution(dailyContributions[0].DateTime, contributionStatus)
                    {
                        HourlyContributions = dailyContributions
                    };

                    monthlyContribution.DailyContributions.Add(dailyContribution);
                }

                yearlyContribution.MonthlyContributions.Add(monthlyContribution);
            }

            Groups.Add(yearlyContribution);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}

//await _dispatcherService.ExecuteOnUIThreadAsync(() =>
//    {
//        //foreach (var contribution in contributions)
//        //{
//        //    Groups.Add(contribution);
//        //}
//    }
//).ConfigureAwait(false);