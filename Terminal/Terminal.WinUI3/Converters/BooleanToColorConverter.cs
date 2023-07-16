using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Terminal.WinUI3.Converters;

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool && (bool)value)
        {
            return new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            return new SolidColorBrush(Colors.LightSalmon);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
