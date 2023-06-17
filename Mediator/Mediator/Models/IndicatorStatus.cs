using System.ComponentModel;
using System.Runtime.CompilerServices;
using Common.Entities;

namespace Mediator.Models;

public sealed class IndicatorStatus : INotifyPropertyChanged
{
    public int Index
    {
        get; init;
    }

    public Symbol Symbol => (Symbol)Enum.GetValues(typeof(Symbol)).GetValue(Index)!;

    public double PipSize;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value)
            {
                return;
            }

            _isConnected = value;
            OnPropertyChanged();
        }
    }

    private Workplace _workplace;
    public Workplace Workplace
    {
        get => _workplace;
        set
        {
            if (_workplace == value)
            {
                return;
            }

            _workplace = value;
            OnPropertyChanged();
        }
    }

    private DateTime _dateTime;
    public DateTime DateTime
    {
        get => _dateTime;
        set
        {
            if (_dateTime.Equals(value))
            {
                return;
            }

            _dateTime = value;
            OnPropertyChanged();
        }
    }

    private double _ask;
    public double Ask
    {
        get => _ask;
        set
        {
            if (_ask.Equals(value))
            {
                return;
            }

            _ask = value;
            OnPropertyChanged();
            OnPropertyChanged("SpreadInPips");
        }
    }

    private double _bid;
    public double Bid
    {
        get => _bid;
        set
        {
            if (_bid.Equals(value))
            {
                return;
            }

            _bid = value;
            OnPropertyChanged();
            OnPropertyChanged("SpreadInPips");
        }
    }

    public string SpreadInPips
    {
        get
        {
            var spreadInPips = (_ask - _bid) / PipSize;
            return spreadInPips.ToString("##.00");
        }
    } 

    private int _counter;
    public int Counter
    {
        get => _counter;
        set
        {
            if (_counter.Equals(value))
            {
                return;
            }

            _counter = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString()
    {
        return $"{Symbol}, {IsConnected}";
    }
}
