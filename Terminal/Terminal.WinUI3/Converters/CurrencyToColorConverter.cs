using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Terminal.WinUI3.Converters;

public class CurrencyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var currencyStr = value as string;

        switch (currencyStr)
        {
            case "USD": return new SolidColorBrush(Colors.LimeGreen);
            case "EUR": return new SolidColorBrush(Color.FromArgb(255, 108, 181, 255));
            case "GBP": return new SolidColorBrush(Colors.MediumPurple);
            case "JPY": return new SolidColorBrush(Colors.Goldenrod);
            default: return new SolidColorBrush(Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}