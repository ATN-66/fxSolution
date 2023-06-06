/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                        TicksOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.ViewModels;

public partial class TicksOverviewViewModel : ObservableRecipient
{
    [ObservableProperty] private ObservableCollection<YearlyContribution> _groups;

    public TicksOverviewViewModel(IDataService dataService)
    {
        _groups = new ObservableCollection<YearlyContribution>();
        var input = dataService.GetTicksContributions();

        var groupedByYear = input
            .GroupBy(c => c.Date.Year)
            .Select(yearGroup => new YearlyContribution
            {
                Year = yearGroup.Key,
                Months = yearGroup
                    .GroupBy(c => c.Date.Month)
                    .Select(monthGroup => new MonthlyContribution
                    {
                        Month = monthGroup.Key,
                        DailyContributions = monthGroup.ToList()
                    })
                    .ToList()
            });

        foreach (var year in groupedByYear)
        {
            _groups.Add(year);
        }
    }
}