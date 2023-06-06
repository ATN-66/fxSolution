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
    [ObservableProperty] private ObservableCollection<MonthlyContributions> _groups;

    public TicksOverviewViewModel(IDataService dataService)
    {
        var input = dataService.GetTicksContributions();
        var groupedContributions = input.GroupBy(c => new { c.Date.Year, c.Date.Month }).ToList();

        _groups = new ObservableCollection<MonthlyContributions>();


        foreach (var group in groupedContributions)
        {
            _groups.Add(new MonthlyContributions
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Contributions = group.ToList()
            });
        }
    }
}