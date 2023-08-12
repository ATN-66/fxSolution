using Microsoft.UI.Xaml.Data;

namespace Terminal.WinUI3.Converters;
public class BoolToIsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isEnabled)
        {
            return isEnabled;
        }
        return false; // Default value if the conversion fails
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isEnabled)
        {
            return isEnabled;
        }
        return false; // Default value if the conversion fails
    }
}
