using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;

namespace Terminal.WinUI3.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue && targetType == typeof(Visibility))
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        throw new InvalidOperationException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}