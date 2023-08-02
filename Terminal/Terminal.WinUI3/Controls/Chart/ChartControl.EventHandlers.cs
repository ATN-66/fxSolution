/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                    ChartControl.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Contracts.Models;

namespace Terminal.WinUI3.Controls.Chart;

public abstract partial class ChartControl<TItem, TKernel> where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected override void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!IsMouseDown)
        {
            return;
        }

        var currentMousePosition = e.GetCurrentPoint(GraphCanvas).Position;
        var currentMouseY = (float)currentMousePosition.Y;
        var currentMouseX = (float)currentMousePosition.X;

        var deltaY = PreviousMouseY - currentMouseY;
        deltaY = IsReversed ? -deltaY : deltaY;
        var deltaX = PreviousMouseX - currentMouseX;

        var pipsChange = deltaY / VerticalScale;
        var unitsChange = deltaX switch
        {
            > 0 => (int)Math.Floor(deltaX / HorizontalScale),
            < 0 => (int)Math.Ceiling(deltaX / HorizontalScale),
            _ => 0
        };

        if (Math.Abs(pipsChange) < 1 && Math.Abs(unitsChange) < 1)
        {
            return;
        }

        if (_slLine is { IsSelected: true } && IsPointOnLine(currentMousePosition, _slLine, ProximityThresholdDynamic))
        {
            if (IsReversed)
            {
                _slLine.StartPoint.Y += (float)deltaY;
                _slLine.EndPoint.Y += (float)deltaY;
                _sl = YZeroPrice - (GraphHeight - _slLine.StartPoint.Y) * Digits / VerticalScale;
            }
            else
            {
                _slLine.StartPoint.Y -= (float)deltaY;
                _slLine.EndPoint.Y -= (float)deltaY;
                _sl = YZeroPrice - _slLine.StartPoint.Y * Digits / VerticalScale;
            }
        }
        else if (_tpLine is { IsSelected: true } && IsPointOnLine(currentMousePosition, _tpLine, ProximityThresholdDynamic))
        {
            if (IsReversed)
            {
                _tpLine.StartPoint.Y += (float)deltaY;
                _tpLine.EndPoint.Y += (float)deltaY;
                _tp = YZeroPrice - (GraphHeight - _tpLine.StartPoint.Y) * Digits / VerticalScale;
            }
            else
            {
                _tpLine.StartPoint.Y -= (float)deltaY;
                _tpLine.EndPoint.Y -= (float)deltaY;
                _tp = YZeroPrice - _tpLine.StartPoint.Y * Digits / VerticalScale;
            }
        }
        else
        {
            VerticalShift += pipsChange;

            switch (HorizontalShift)
            {
                case 0 when KernelShift == 0:
                    switch (unitsChange)
                    {
                        case > 0:
                            HorizontalShift += unitsChange;
                            HorizontalShift = Math.Clamp(HorizontalShift, 0, Units - 1);
                            break;
                        case < 0:
                            KernelShift -= unitsChange;
                            KernelShift = Math.Clamp(KernelShift, 0, Math.Max(0, Kernel.Count - Units));
                            break;
                    }

                    break;
                case > 0:
                    Debug.Assert(KernelShift == 0);
                    HorizontalShift += unitsChange;
                    HorizontalShift = Math.Clamp(HorizontalShift, 0, Units - 1);
                    break;
                default:
                {
                    if (KernelShift > 0)
                    {
                        Debug.Assert(HorizontalShift == 0);
                        KernelShift -= unitsChange;
                        KernelShift = Math.Clamp(KernelShift, 0, Math.Max(0, Kernel.Count - Units));
                    }
                    else
                    {
                        throw new InvalidOperationException("_horizontalShift <= 0 && _kernelShiftValue <= 0");
                    }

                    break;
                }
            }
        }

        Invalidate();
        PreviousMouseY = currentMouseY;
        PreviousMouseX = currentMouseX;
    }
}