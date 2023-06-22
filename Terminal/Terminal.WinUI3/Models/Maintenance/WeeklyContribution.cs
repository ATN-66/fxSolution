/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                            WeeklyContribution.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Terminal.WinUI3.Models.Maintenance;

public sealed class WeeklyContribution : INotifyPropertyChanged
{
    private readonly ObservableCollection<DailyContribution> _dailyContributions = null!;

    public int Year => DailyContributions[0].Year;

    public int Week => DailyContributions[0].Week;

    public ObservableCollection<DailyContribution> DailyContributions
    {
        get => _dailyContributions;
        init
        {
            if (_dailyContributions == value)
            {
                return;
            }

            _dailyContributions = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null!) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}