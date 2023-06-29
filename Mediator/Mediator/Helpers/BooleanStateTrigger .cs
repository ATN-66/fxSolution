using Microsoft.UI.Xaml;

namespace Mediator.Helpers;

public class BooleanStateTrigger : StateTriggerBase
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(bool), typeof(BooleanStateTrigger), new PropertyMetadata(false, OnValuePropertyChanged));

    public bool Value
    {
        get => (bool)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var obj = (BooleanStateTrigger)d;
        var val = (bool)e.NewValue;
        obj.SetActive(val);
    }
}