using Microsoft.Graphics.Canvas.Geometry;
using System.Globalization;
using System.Numerics;
using Windows.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.AI.Data;

namespace Terminal.WinUI3.Controls;

public class BaseChartControl : Control
{
    private Kernel _kernel;
    private CanvasControl? _canvas;
    private const float DataStrokeThickness = 1;
    private readonly List<double> _data = new();
    private readonly Random _rand = new();
    private double _lastValue = 0.5;

    public BaseChartControl(Kernel kernel)
    {
        _kernel = kernel;
        DefaultStyleKey = typeof(BaseChartControl);
    }
    
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _canvas = GetTemplateChild("canvas") as CanvasControl;
        _canvas!.Draw += Canvas_OnDraw;
    }

    private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        for (var i = 0; i < 10; i++)
        {
            var delta = _rand.NextDouble() * .1 - .05;
            _lastValue = Math.Max(0d, Math.Min(1d, _lastValue + delta));
            _data.Add(_lastValue);
        }

        if (_data.Count > (int)_canvas.ActualWidth)
        {
            _data.RemoveRange(0, _data.Count - (int)_canvas.ActualWidth);
        }

        args.DrawingSession.Clear(Colors.Black);

        RenderAxes(_canvas, args);
        RenderData(_canvas, args, Colors.White, DataStrokeThickness, _data);

        _canvas.Invalidate();
    }

    private static void RenderAxes(CanvasControl? canvas, CanvasDrawEventArgs args)
    {
        var width = (float)canvas.ActualWidth;
        var height = (float)canvas.ActualHeight;
        var midWidth = (float)(width * .5);
        var midHeight = (float)(height * .5);

        using (var cpb = new CanvasPathBuilder(args.DrawingSession))
        {
            // Horizontal line
            cpb.BeginFigure(new Vector2(0, midHeight));
            cpb.AddLine(new Vector2(width, midHeight));
            cpb.EndFigure(CanvasFigureLoop.Open);

            // Horizontal line arrow
            cpb.BeginFigure(new Vector2(width - 10, midHeight - 3));
            cpb.AddLine(new Vector2(width, midHeight));
            cpb.AddLine(new Vector2(width - 10, midHeight + 3));
            cpb.EndFigure(CanvasFigureLoop.Open);

            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Gray, 1);
        }

        args.DrawingSession.DrawText("0", 5, midHeight - 30, Colors.Gray);
        args.DrawingSession.DrawText(canvas.ActualWidth.ToString(CultureInfo.InvariantCulture), width - 50,
            midHeight - 30, Colors.Gray);

        using (var cpb = new CanvasPathBuilder(args.DrawingSession))
        {
            // Vertical line
            cpb.BeginFigure(new Vector2(midWidth, 0));
            cpb.AddLine(new Vector2(midWidth, height));
            cpb.EndFigure(CanvasFigureLoop.Open);

            // Vertical line arrow
            cpb.BeginFigure(new Vector2(midWidth - 3, 10));
            cpb.AddLine(new Vector2(midWidth, 0));
            cpb.AddLine(new Vector2(midWidth + 3, 10));
            cpb.EndFigure(CanvasFigureLoop.Open);

            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Gray, 1);
        }

        args.DrawingSession.DrawText("0", midWidth + 5, height - 30, Colors.Gray);
        args.DrawingSession.DrawText("1", midWidth + 5, 5, Colors.Gray);
    }

    private static void RenderData(CanvasControl? canvas, CanvasDrawEventArgs args, Color color, float thickness, List<double> data)
    {
        using var cpb = new CanvasPathBuilder(args.DrawingSession);
        cpb.BeginFigure(new Vector2(0, (float)(canvas.ActualHeight * (1 - data[0]))));
        for (var i = 1; i < data.Count; i++) cpb.AddLine(new Vector2(i, (float)(canvas.ActualHeight * (1 - data[i]))));
        cpb.EndFigure(CanvasFigureLoop.Open);
        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), color, thickness);
    }

    public void Detach()
    {
        if (_canvas != null)
        {
            _canvas.Draw -= Canvas_OnDraw;
        }
    }
}