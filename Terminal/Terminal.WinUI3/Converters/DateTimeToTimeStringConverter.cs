using Microsoft.UI.Xaml.Data;

namespace Terminal.WinUI3.Converters;

public class DateTimeToTimeStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return $"{dateTime:t}";
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}