using Common.ExtensionsAndHelpers;
using Microsoft.UI.Xaml.Data;

namespace Terminal.WinUI3.Converters;

public class DateTimeToDateStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return $"{dateTime:dddd, yyyy MMMM dd}, (week:{dateTime.Week()})";
        }
        if (value is DateTimeOffset dateTimeOffset)
        {
            return $"{dateTimeOffset:dddd, yyyy MMMM dd}, (week:{dateTimeOffset.DateTime.Week()})";
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}