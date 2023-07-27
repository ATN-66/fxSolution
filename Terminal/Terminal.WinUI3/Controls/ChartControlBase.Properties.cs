/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                   ChartControlBase.Properties.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;

namespace Terminal.WinUI3.Controls;

public abstract partial class ChartControlBase
{
    protected double GraphWidth;
    protected double GraphHeight;
    protected double HorizontalScale;
    protected double VerticalScale;
    
    protected double VerticalShift;

    public static readonly DependencyProperty MinPipsProperty = DependencyProperty.Register(nameof(MinPips), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public static readonly DependencyProperty MaxPipsProperty = DependencyProperty.Register(nameof(MaxPips), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public static readonly DependencyProperty PipsPercentProperty = DependencyProperty.Register(nameof(PipsPercent), typeof(int), typeof(ChartControlBase), new PropertyMetadata(0));
    public int MinPips
    {
        get => (int)GetValue(MinPipsProperty);
        set => SetValue(MinPipsProperty, value);
    }
    public int MaxPips
    {
        get => (int)GetValue(MaxPipsProperty);
        set => SetValue(MaxPipsProperty, value);
    }
    public int PipsPercent
    {
        get => (int)GetValue(PipsPercentProperty);
        set
        {
            if (value.Equals(PipsPercent))
            {
                return;
            }

            SetValue(PipsPercentProperty, value);
            Pips = Math.Max(MinPips, MaxPips * PipsPercent / 100);
        }
    }
    private int _pips;
    protected int Pips
    {
        get => _pips;
        set
        {
            if (_units == value)
            {
                return;
            }

            _pips = value;
            OnPipsChanged();
        }
    }
    private void OnPipsChanged()
    {
        VerticalScale = GraphHeight / Pips;
        GraphCanvas!.Invalidate();
        YAxisCanvas!.Invalidate();
        XAxisCanvas!.Invalidate();
        DebugCanvas!.Invalidate();
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
        set
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
            _kernelShift = CalculateKernelShift();

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
            DebugCanvas!.Invalidate();
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
}