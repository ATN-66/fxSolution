using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Terminal.WinUI3.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hasContribution = (bool)value;
        return hasContribution ? new SolidColorBrush(Colors.PaleGreen) : new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}