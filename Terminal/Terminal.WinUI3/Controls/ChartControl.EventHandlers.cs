/*+------------------------------------------------------------------+
  |                                         Terminal.WinUI3.Controls |
  |                                    ChartControl.EventHandlers.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.AI.Data;

namespace Terminal.WinUI3.Controls;
public abstract partial class ChartControl<TItem, TKernel> where TItem : IChartItem where TKernel : IKernel<TItem>
{
    protected override void GraphCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (!IsMouseDown)
            {
                return;
            }

            var currentMouseY = (float)e.GetCurrentPoint(GraphCanvas).Position.Y;

            var deltaY = PreviousMouseY - currentMouseY;
            deltaY = IsReversed ? -deltaY : deltaY;

            var pipsChange = deltaY / VerticalScale;

            var currentMouseX = (float)e.GetCurrentPoint(GraphCanvas).Position.X;
            var deltaX = PreviousMouseX - currentMouseX;
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

            GraphCanvas!.Invalidate();
            YAxisCanvas!.Invalidate();
            XAxisCanvas!.Invalidate();
            DebugCanvas!.Invalidate();
            PreviousMouseY = currentMouseY;
            PreviousMouseX = currentMouseX;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(Logger, exception, "GraphCanvas_OnPointerMoved");
            throw;
        }
    }
}
