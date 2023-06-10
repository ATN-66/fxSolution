/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                            YearlyContribution.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Terminal.WinUI3.Models.Maintenance;

public sealed class YearlyContribution : INotifyPropertyChanged
{
    private readonly ObservableCollection<MonthlyContribution>? _monthlyContributions;
    private readonly ObservableCollection<WeeklyContribution>? _weeklyContributions;

    public int Year
    {
        get; init;
    }

    public ObservableCollection<MonthlyContribution>? MonthlyContributions
    {
        get => _monthlyContributions;
        init
        {
            if (_monthlyContributions == value)
            {
                return;
            }

            _monthlyContributions = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<WeeklyContribution>? WeeklyContributions
    {
        get => _weeklyContributions;
        init
        {
            if (_weeklyContributions == value)
            {
                return;
            }

            _weeklyContributions = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null!) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}