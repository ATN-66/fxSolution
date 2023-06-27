using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Mediator.Converters;

public class BigBoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        switch (value)
        {
            case bool boolValue when boolValue:
                return new SolidColorBrush(Colors.PaleGreen); // ON state
            case bool boolValue when !boolValue:
                return new SolidColorBrush(Colors.PaleVioletRed); // OFF state
            case null:
                return new SolidColorBrush(Colors.Yellow); // FAULT state
            default:
                throw new InvalidOperationException("Must be a boolean or null value.");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}