/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                             DailyContribution.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Terminal.WinUI3.Models.Maintenance;

public class DailyContribution : INotifyPropertyChanged
{
    public int Year
    {
        get; init;
    }

    public int Month
    {
        get; init;
    }

    public int Week
    {
        get; init;
    }

    public int Day
    {
        get; init;
    }

    private Contribution? _contribution;
    public Contribution? Contribution
    {
        get => _contribution;
        set
        {
            if (_contribution == value)
            {
                return;
            }

            _contribution = value;
            OnPropertyChanged();
        }
    }

    public List<HourlyContribution> HourlyContributions
    {
        get; set;
    } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString()
    {
        return $"year:{Year}, month:{Month}, week:{Week}, day:{Day}, contribution:{Contribution}";
    }
}