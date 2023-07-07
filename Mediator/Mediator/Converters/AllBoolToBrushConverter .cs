using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Mediator.Converters;

public class BigBoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            bool boolValue when boolValue => new SolidColorBrush(Colors.PaleGreen),
            bool boolValue when !boolValue => new SolidColorBrush(Colors.PaleVioletRed),
            null => new SolidColorBrush(Colors.Yellow),
            _ => throw new InvalidOperationException("Must be a boolean or null value.")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}