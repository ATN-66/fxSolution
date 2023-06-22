/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                           MonthlyContribution.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Terminal.WinUI3.Models.Maintenance;

public sealed class MonthlyContribution : INotifyPropertyChanged
{
    private readonly ObservableCollection<DailyContribution> _dailyContributions = null!;

    public int Year => DailyContributions[0].Year;

    public int Month => DailyContributions[0].Month;

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