/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                              ChartControlBaseFirst.Properties.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Models.Chart;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase
{
    public readonly ChartType ChartType;
    protected readonly bool IsReversed;
    protected readonly double Digits;
    protected int DecimalPlaces
    {
        get
        {
            var value = Digits;
            var count = 0;
            while (value < 1)
            {
                value *= 10;
                count++;
            }
            return count;
        }
    }

    private double TickValue
    {
        get;
        set;
    }
    private const double Century = 100d; // todo: settings // one hundred dollars of the balance currency
    private double _pipsPerCentury;
    protected readonly ViewPort ViewPort = new();

    protected double GraphWidth;
    protected double GraphHeight;
    protected double HorizontalScale;
    protected double VerticalScale;
    protected int Pips;

    public ChartSettings GetChartSettings()
    {
        var result = new ChartSettings()
        {
            ChartType = ChartType,
            Symbol = Symbol,
            IsReversed = IsReversed,
            HorizontalShift = HorizontalShift,
            VerticalShift = VerticalShift,
            KernelShiftPercent = KernelShiftPercent
        };

        return result;
    }
    
    public static readonly DependencyProperty MinCenturiesProperty = DependencyProperty.Register(nameof(MinCenturies), typeof(double), typeof(ChartControlBase), new PropertyMetadata(0d));
    public double MinCenturies
    {
        get => (double)GetValue(MinCenturiesProperty);
        set => throw new InvalidOperationException("The set method for MinCenturies should not be called.");
    }
    public static readonly DependencyProperty MaxCenturiesProperty = DependencyProperty.Register(nameof(MaxCenturies), typeof(double), typeof(ChartControlBase), new PropertyMetadata(0d));
    public double MaxCenturies
    {
        get => (double)GetValue(MaxCenturiesProperty);
        set => throw new InvalidOperationException("The set method for MaxCenturies should not be called.");
    }
    public static readonly DependencyProperty CenturiesPercentProperty = DependencyProperty.Register(nameof(CenturiesPercent), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public int CenturiesPercent
    {
        get => (int)GetValue(CenturiesPercentProperty);
        set
        {
            if (value.Equals(CenturiesPercent))
            {
                return;
            }

            SetValue(CenturiesPercentProperty, value);
            Centuries = MinCenturies + (MaxCenturies - MinCenturies) * CenturiesPercent / 100d;
        }
    }
    private double _centuries;
    protected double Centuries
    {
        get => _centuries;
        private set
        {
            if (value.Equals(_centuries))
            {
                return;
            }

            _centuries = value;
            OnCenturiesChanged();
        }
    }
    private void OnCenturiesChanged()
    {
        var pipsPerCentury = Century / TickValue / 10d;
        Pips = (int)(pipsPerCentury * Centuries);
        VerticalScale = GraphHeight / Pips;

        EnqueueMessage(MessageType.Trace, $"H: {GraphHeight}, TV: {TickValue}, Cs: {Centuries:0.000}, Ps: {Pips}, VS: {VerticalShift}");
        Invalidate();
    }

    public static readonly DependencyProperty MinUnitsProperty = DependencyProperty.Register(nameof(MinUnits), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public static readonly DependencyProperty UnitsPercentProperty = DependencyProperty.Register(nameof(UnitsPercent), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public int MinUnits
    {
        get => (int)GetValue(MinUnitsProperty);
        set
        {
            if (MinUnits == value)
            {
                return;
            }

            SetValue(MinUnitsProperty, value);
        }
    }
    protected int MaxUnits;
    public int UnitsPercent
    {
        get => (int)GetValue(UnitsPercentProperty);
        set
        {
            if (UnitsPercent == value)
            {
                return;
            }

            SetValue(UnitsPercentProperty, value);
            Units = Math.Max(MinUnits, MaxUnits * UnitsPercent / 100);
        }
    }
    private int _units;
    protected int Units
    {
        get => _units;
        private set
        {
            if (_units == value)
            {
                return;
            }

            _units = value;
            OnUnitsChanged();
        }
    }
    protected abstract void OnUnitsChanged();

    public static readonly DependencyProperty KernelShiftProperty = DependencyProperty.Register(nameof(KernelShift), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    protected int KernelShift
    {
        get => (int)GetValue(KernelShiftProperty);
        set
        {
            if (value.Equals(KernelShift))
            {
                return;
            }
            SetValue(KernelShiftProperty, value);
            SetValue(KernelShiftPercentProperty, CalculateKernelShiftPercent());
        }
    }
    public void OnKernelShift(int value)
    {
        if (ChartType != ChartType.Candlesticks)
        {
            return;
        }
        KernelShift = value;
        Invalidate();
    }

    public static readonly DependencyProperty KernelShiftPercentProperty = DependencyProperty.Register(nameof(KernelShiftPercent), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public int KernelShiftPercent
    {
        get => (int)GetValue(KernelShiftPercentProperty);
        set
        {
            if (value.Equals(KernelShiftPercent))
            {
                return;
            }
            SetValue(KernelShiftPercentProperty, value);
            HorizontalShift = 0;
            SetValue(KernelShiftProperty, CalculateKernelShift());
            Invalidate();
        }
    }
    protected abstract int CalculateKernelShift();
    protected abstract int CalculateKernelShiftPercent();

    public static readonly DependencyProperty HorizontalShiftProperty = DependencyProperty.Register(nameof(HorizontalShift), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public int HorizontalShift
    {
        get => (int)GetValue(HorizontalShiftProperty);
        set => SetValue(HorizontalShiftProperty, value);
    }
    public void OnHorizontalShift(int value)
    {
        if (ChartType != ChartType.Candlesticks)
        {
            return;
        }
        HorizontalShift = value;
        Invalidate();
    }

    public static readonly DependencyProperty VerticalShiftProperty = DependencyProperty.Register(nameof(VerticalShift), typeof(double), typeof(ChartControlBase), new PropertyMetadata(0d));
    public double VerticalShift
    {
        get => (double)GetValue(VerticalShiftProperty);
        set
        {
            if (value.Equals(VerticalShift))
            {
                return;
            }
            SetValue(VerticalShiftProperty, value);
            CenturyShift = VerticalShift / _pipsPerCentury;
        }
    }
    public static readonly DependencyProperty CenturyShiftProperty = DependencyProperty.Register(nameof(CenturyShift), typeof(double), typeof(ChartControlBase), new PropertyMetadata(0d));
    public double CenturyShift
    {
        get => (double)GetValue(CenturyShiftProperty);
        set => SetValue(CenturyShiftProperty, value);
    }
    public void OnCenturyShift(bool isReversed, double value)
    {
        if (IsReversed != isReversed)
        {
            value = -value;
        }

        SetValue(CenturyShiftProperty, value);
        SetValue(VerticalShiftProperty, value * _pipsPerCentury);
        Invalidate();
    }

    public static readonly DependencyProperty IsVerticalLineRequestedProperty = DependencyProperty.Register(nameof(IsVerticalLineRequested), typeof(bool), typeof(ChartControlBase), new PropertyMetadata(false));
    public bool IsVerticalLineRequested
    {
        get => (bool)GetValue(IsVerticalLineRequestedProperty);
        set
        {
            if (IsVerticalLineRequested == value)
            {
                return;
            }
            SetValue(IsVerticalLineRequestedProperty, value);
            ProtectedCursor = InputSystemCursor.Create(value ? InputSystemCursorShape.Cross : InputSystemCursorShape.Arrow);
        }
    }

    public static readonly DependencyProperty IsHorizontalLineRequestedProperty = DependencyProperty.Register(nameof(IsHorizontalLineRequested), typeof(bool), typeof(ChartControlBase), new PropertyMetadata(false));
    public bool IsHorizontalLineRequested
    {
        get => (bool)GetValue(IsHorizontalLineRequestedProperty);
        set
        {
            if (IsHorizontalLineRequested == value)
            {
                return;
            }
            SetValue(IsHorizontalLineRequestedProperty, value);
            ProtectedCursor = InputSystemCursor.Create(value ? InputSystemCursorShape.Cross : InputSystemCursorShape.Arrow);
        }
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ChartControlBase), new PropertyMetadata(false));
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set
        {
            if (IsSelected == value)
            {
                return;
            }

            try // memory violation in WPF
            {
                SetValue(IsSelectedProperty, value);
                OnIsSelectedChanged();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
           
        }
    }
    private void OnIsSelectedChanged()
    {
        _textCanvas!.Invalidate();
    }
}