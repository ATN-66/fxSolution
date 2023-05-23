using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal.WinUI3.Controls;

public class BaseChartControl : Control
{
    //private CanvasControl rootCanvas = null;

    public BaseChartControl()
    {
        DefaultStyleKey = typeof(BaseChartControl);
    }

    public bool HasLabelValue
    {
        get; set;
    }

    public string Label
    {
        get => (string)GetValue(_labelProperty);
        set => SetValue(_labelProperty, value);
    }

    private readonly DependencyProperty _labelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(BaseChartControl),
        new PropertyMetadata(default(string), new PropertyChangedCallback(OnLabelChanged)));

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var labelControl = d as BaseChartControl;
        var s = e.NewValue as string;
        labelControl!.HasLabelValue = s != string.Empty;
    }
}