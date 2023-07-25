﻿using Windows.UI;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Animation;

namespace Terminal.WinUI3.Controls;

public static class CompositionGradientBrushExtensions
{
    public static void CreateColorStopsWithEasingFunction(this CompositionGradientBrush compositionGradientBrush, EasingType easingType, EasingMode easingMode, float colorStopBegin, float colorStopEnd, float gap = 0.05f)
    {
        var compositor = compositionGradientBrush.Compositor;
        var easingFunc = easingType.ToEasingFunction(easingMode);
        if (easingFunc != null)
        {
            for (var i = colorStopBegin; i < colorStopEnd; i += gap)
            {
                var progress = (i - colorStopBegin) / (colorStopEnd - colorStopBegin);

                var colorStop = compositor.CreateColorGradientStop(i, Color.FromArgb((byte)(0xff * easingFunc.Ease(1 - progress)), 0, 0, 0));
                compositionGradientBrush.ColorStops.Add(colorStop);
            }
        }
        else
        {
            compositionGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(colorStopBegin, Colors.Black));
        }

        compositionGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(colorStopEnd, Colors.Transparent));
    }
}