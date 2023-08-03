using Windows.UI;
using Common.Entities;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Terminal.WinUI3.Converters;

public class CurrencyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var currency = (Currency)value;

        return currency switch
        {
            Currency.USD => new SolidColorBrush(Colors.LimeGreen),
            Currency.EUR => new SolidColorBrush(Color.FromArgb(255, 108, 181, 255)),
            Currency.GBP => new SolidColorBrush(Colors.MediumPurple),
            Currency.JPY => new SolidColorBrush(Colors.Goldenrod),
            Currency.NaN => new SolidColorBrush(Colors.Transparent),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}