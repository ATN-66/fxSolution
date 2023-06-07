/*+------------------------------------------------------------------+
  |                                       Terminal.WinUI3.Converters |
  |                                  ContributionToColorConverter.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Converters;

public class ContributionToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var contribution = (Contribution)value;
        return contribution switch
        {
            Contribution.None => new SolidColorBrush(Colors.LightGray),
            Contribution.Partial => new SolidColorBrush(Colors.PaleTurquoise),
            Contribution.Full => new SolidColorBrush(Colors.PaleGreen),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}