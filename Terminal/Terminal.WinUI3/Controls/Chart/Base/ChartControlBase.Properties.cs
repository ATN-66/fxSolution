/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                   ChartControlBaseFirst.Properties.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;
using Terminal.WinUI3.Models.Chart;

namespace Terminal.WinUI3.Controls.Chart.Base;

public abstract partial class ChartControlBase
{
    protected double GraphWidth;
    protected double GraphHeight;
    protected double HorizontalScale;
    protected double VerticalScale;
    protected double VerticalShift;
    protected int Pips;

    public static readonly DependencyProperty MinCenturiesProperty = DependencyProperty.Register(nameof(MinCenturies), typeof(double), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
    public double MinCenturies
    {
        get => (double)GetValue(MinCenturiesProperty);
        set => throw new InvalidOperationException("The set method for MinCenturies should not be called.");
    }
    public static readonly DependencyProperty MaxCenturiesProperty = DependencyProperty.Register(nameof(MaxCenturies), typeof(double), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
    public double MaxCenturies
    {
        get => (double)GetValue(MaxCenturiesProperty);
        set => throw new InvalidOperationException("The set method for MaxCenturies should not be called.");
    }
    public static readonly DependencyProperty CenturiesPercentProperty = DependencyProperty.Register(nameof(CenturiesPercent), typeof(int), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
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

    public static readonly DependencyProperty MinUnitsProperty = DependencyProperty.Register(nameof(MinUnits), typeof(int), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
    public static readonly DependencyProperty UnitsPercentProperty = DependencyProperty.Register(nameof(UnitsPercent), typeof(int), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
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

    private int _kernelShift;
    protected int KernelShift
    {
        get => _kernelShift;
        set
        {
            if (value.Equals(_kernelShift))
            {
                return;
            }
            _kernelShift = value;
            SetValue(KernelShiftPercentProperty, CalculateKernelShiftPercent());
        }
    }
    public static readonly DependencyProperty KernelShiftPercentProperty = DependencyProperty.Register(nameof(KernelShiftPercent), typeof(int), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
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
            _kernelShift = CalculateKernelShift();

            Invalidate();
        }
    }
    protected abstract int CalculateKernelShift();
    protected abstract int CalculateKernelShiftPercent();

    public static readonly DependencyProperty HorizontalShiftProperty = DependencyProperty.Register(nameof(HorizontalShift), typeof(int), typeof(Chart.Base.ChartControlBase), new PropertyMetadata(0));
    public int HorizontalShift
    {
        get => (int)GetValue(HorizontalShiftProperty);
        set => SetValue(HorizontalShiftProperty, value);
    }
}