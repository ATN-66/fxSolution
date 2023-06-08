/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly IDataService _dataService;
    [ObservableProperty] private ObservableCollection<YearlyContribution> _groups = new();
    [ObservableProperty] private string _headerContext = "Ticks Overview";
    private CancellationTokenSource _cts = null!;

    public TicksOverviewViewModel(IDataService dataService)
    {
        _dataService = dataService;
        ContributeTicksCommand = new AsyncRelayCommand(ContributeTicksAsync);
    }

    public IAsyncRelayCommand ContributeTicksCommand
    {
        get;
    }

    public async void OnNavigatedTo(object parameter)
    {
        var contributions = await _dataService.GetTicksContributionsAsync().ConfigureAwait(true);
        UpdateGroups(contributions);
        Messenger.Register<TicksOverviewViewModel, DataServiceMessage, Party>(this, Party.Blanket, (_, m) => //todo: party
        {
            OnDataServiceMessageReceived(m);
        });
    }

    public void OnNavigatedFrom()
    {
        _cts.Cancel();
        Messenger.Unregister<DataServiceMessage, Party>(this, Party.Blanket);//todo: party
    }

    private void UpdateGroups(IEnumerable<HourlyContribution> contributions)
    {
        Groups.Clear();

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
            var yearlyContribution = new YearlyContribution
            {
                Year = yearGroup.Year,
                MonthlyContributions = new List<MonthlyContribution>()
            };

            // Iterate over each month in the year
            foreach (var monthGroup in yearGroup.Months)
            {
                var monthlyContribution = new MonthlyContribution
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


    private async Task ContributeTicksAsync()
    {
        using (_cts = new CancellationTokenSource())
        {
            try
            {
                await _dataService.ContributeTicksAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e.Message);
                // Handle cancellation here.
                // For example, if you have a status label in the UI, you might do something like:
                // this.StatusLabel.Text = "Operation cancelled";
            }
        }
    }

    private void OnDataServiceMessageReceived(DataServiceMessage dataServiceMessage)
    {
        Debug.WriteLine(dataServiceMessage.ToString());
    }
}