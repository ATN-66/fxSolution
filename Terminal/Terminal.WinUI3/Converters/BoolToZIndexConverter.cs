using Microsoft.UI.Xaml.Data;

namespace Terminal.WinUI3.Converters;
public class BoolToZIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? 1 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}